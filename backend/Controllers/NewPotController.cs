namespace MagicPot.Backend.Controllers
{
    using System.ComponentModel.DataAnnotations;
    using System.Net.Mime;
    using MagicPot.Backend.Attributes;
    using MagicPot.Backend.Data;
    using MagicPot.Backend.Services;
    using Microsoft.AspNetCore.Mvc;
    using SixLabors.ImageSharp;
    using Swashbuckle.AspNetCore.Annotations;
    using TonLibDotNet;
    using TonLibDotNet.Cells;

    [ApiController]
    [Produces(MediaTypeNames.Application.Json)]
    [Route("/api/[controller]/[action]")]
    [SwaggerResponse(200, "Request is accepted, processed and response contains requested data.")]
    public class NewPotController(
        CachedData cachedData,
        Lazy<IDbProvider> lazyDbProvider,
        ILogger<NewPotController> logger,
        IFileService fileService)
        : ControllerBase
    {
        private const int MaxRetries = 100;
        private static readonly Random Rand = new();

        [HttpPost]
        [Consumes(MediaTypeNames.Application.Json)]
        public async Task<CheckResult> CheckNewPotAsJson(
            [Required, InitDataValidation, FromHeader(Name = BackendOptions.TelegramInitDataHeaderName)] string initData,
            [Required] NewPotWithCoverModel model)
        {
            await ValidateJetton(model);
            using var ms = await ValidateCoverImage(model.CoverImage, null, nameof(model.CoverImage));

            return ModelState.IsValid ? new CheckResult(true, null) : new CheckResult(false, new ValidationProblemDetails(ModelState).Errors);
        }

        [HttpPost]
        [Consumes(MediaTypeNames.Multipart.FormData)]
        public async Task<CheckResult> CheckNewPotAsForm(
            [Required, InitDataValidation, FromHeader(Name = BackendOptions.TelegramInitDataHeaderName)] string initData,
            [Required][FromForm] NewPotModel model,
            IFormFile? coverImage)
        {
            await ValidateJetton(model);
            using var ms = await ValidateCoverImage(null, coverImage, nameof(coverImage));

            return ModelState.IsValid ? new CheckResult(true, null) : new CheckResult(false, new ValidationProblemDetails(ModelState).Errors);
        }

        [HttpPost]
        [Consumes(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<NewPotInfo>> CreateNewPotAsJson(
            [Required, InitDataValidation, FromHeader(Name = BackendOptions.TelegramInitDataHeaderName)] string initData,
            [Required] NewPotWithCoverModel model)
        {
            using var ms = await ValidateCoverImage(model.CoverImage, null, nameof(model.CoverImage));

            return await CreateNewPot(initData, model, ms);
        }

        [HttpPost]
        [Consumes(MediaTypeNames.Multipart.FormData)]
        public async Task<ActionResult<NewPotInfo>> CreateNewPotAsForm(
            [Required, InitDataValidation, FromHeader(Name = BackendOptions.TelegramInitDataHeaderName)] string initData,
            [Required][FromForm] NewPotModel model,
            IFormFile? coverImage)
        {
            using var ms = await ValidateCoverImage(null, coverImage, nameof(coverImage));

            return await CreateNewPot(initData, model, ms);
        }

        protected async Task<ActionResult<NewPotInfo>> CreateNewPot(string initData, NewPotModel model, MemoryStream? coverImage)
        {
            var (jetton, userJettonAddress) = await ValidateJetton(model);

            if (!ModelState.IsValid)
            {
                return new BadRequestObjectResult(new ValidationProblemDetails(ModelState));
            }

            var userId = InitDataValidationAttribute.GetUserIdWithoutValidation(initData);

            var pot = await CreatePot(model, userId, jetton!, coverImage);
            var txInfo = PrepareTxInfo(pot, jetton!, userJettonAddress!);

            return new NewPotInfo(pot.Key, txInfo.RawAddress, txInfo.Amount, txInfo.Payload);
        }

        protected async Task<(Jetton? Jetton, string? UserJettonWallet)> ValidateJetton(NewPotModel model)
        {
            if (!string.IsNullOrEmpty(model.TokenName))
            {
                if (!cachedData.Options.WellKnownJettons.TryGetValue(model.TokenName, out var adr))
                {
                    ModelState.AddModelError(nameof(model.TokenName), Messages.UnknownTokenName);
                    return (null, null);
                }

                model.TokenAddress = adr;
            }

            if (string.IsNullOrEmpty(model.TokenAddress))
            {
                ModelState.AddModelError(nameof(model.TokenAddress), Messages.TokenNameOrAddressRequired);
                return (null, null);
            }

            model.TokenAddress = TonLibDotNet.Utils.AddressUtils.Instance.SetBounceable(model.TokenAddress, true);

            var db = lazyDbProvider.Value.MainDb;

            var jetton = db.Find<Jetton>(x => x.Address == model.TokenAddress);
            if (jetton == null)
            {
                var tonapi = HttpContext.RequestServices.GetRequiredService<TonApiService>();
                jetton = await tonapi.GetJettonInfo(model.TokenAddress);
                if (jetton == null)
                {
                    ModelState.AddModelError(nameof(model.TokenAddress), Messages.AddressIsNotAJetton);
                    return (null, null);
                }

                var existing = db.Find<Jetton>(x => x.Address == jetton.Address);
                if (existing == null)
                {
                    db.Insert(jetton);
                    logger.LogInformation("New Jetton saved: {Symbox} {Address} ({Name})", jetton.Symbol, jetton.Address, jetton.Name);
                }

                HttpContext.ReloadCachedData();
            }

            if (string.IsNullOrEmpty(model.UserAddress))
            {
                return (null, null);
            }

            model.UserAddress = TonLibDotNet.Utils.AddressUtils.Instance.SetBounceable(model.UserAddress, false);

            var ujw = db.Find<UserJettonWallet>(x => x.MainWallet == model.UserAddress && x.JettonMaster == jetton.Address);
            if (ujw == null)
            {
                ujw = new UserJettonWallet()
                {
                    MainWallet = model.UserAddress,
                    JettonMaster = jetton.Address,
                };

                db.Insert(ujw);
            }

            if (string.IsNullOrWhiteSpace(ujw.JettonWallet))
            {
                ModelState.AddModelError(nameof(model.TokenAddress), Messages.ValidatingUserJettonWallet);
                return (null, null);
            }

            return (jetton, ujw.JettonWallet);
        }

        protected async Task<MemoryStream?> ValidateCoverImage(string? base64, IFormFile? file, string paramName)
        {
            MemoryStream? image = null;

            if (!string.IsNullOrEmpty(base64))
            {
                try
                {
                    var bytes = Convert.FromBase64String(base64);
                    image = new MemoryStream(bytes);
                }
                catch (FormatException)
                {
                    ModelState.AddModelError(paramName, "Unable to decode base64 image data");
                    return null;
                }
            }

            if (image == null && file != null && file.Length > 0)
            {
                image = new MemoryStream();
                await file.CopyToAsync(image);
                image.Position = 0;
            }

            if (image == null)
            {
                return null;
            }

            try
            {
                using var img = await Image.LoadAsync(image);
                logger.LogDebug("Image OK: {Format}, width {Width}, height {Height}", img.Metadata.DecodedImageFormat?.Name, img.Width, img.Height);
                image.Position = 0;
                return image;
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Image not Ok");
                ModelState.AddModelError(paramName, "Not a valid image, or format not supported");
                return null;
            }
        }

        protected long GeneratePotId()
        {
            var db = lazyDbProvider.Value.MainDb;

            for (var i = 0; i <= MaxRetries; i++)
            {
                var id = Rand.NextInt64(int.MaxValue / 256, (long)int.MaxValue * 1024);
                var existing = db.Find<Pot>(id);
                if (existing == null)
                {
                    return id;
                }
            }

            throw new CreateNewPotException($"Failed to generate Pot Id (in {MaxRetries} attempts)");
        }

        protected PrecachedMnemonic GeneratePotContract()
        {
            var db = lazyDbProvider.Value.MainDb;

            for (var i = 0; i <= MaxRetries; i++)
            {
                var mnemonic = db.Table<PrecachedMnemonic>().First();
                var count = db.Delete(mnemonic);
                if (count == 1)
                {
                    return mnemonic;
                }
            }

            throw new CreateNewPotException($"Failed to generate Pot Contract Address (in {MaxRetries} attempts)");
        }

        protected async Task<Pot> CreatePot(NewPotModel model, long userId, Jetton jetton, MemoryStream? coverImage)
        {
            var db = lazyDbProvider.Value.MainDb;

            var pot = new Pot
            {
                Id = GeneratePotId(),
                Name = model.Name,
                OwnerUserId = userId,
                OwnerUserAddress = model.UserAddress,
                TokenAddress = jetton.Address,
                JettonWalletAddress = string.Empty,
                InitialSize = model.InitialSize,
                Created = DateTimeOffset.UtcNow,
            };

            pot.Key = Base36.Encode(pot.Id);

            if (coverImage != null)
            {
                pot.CoverImage = await fileService.Upload(coverImage, pot.Key + ".dat");
                logger.LogDebug("Cover image uploaded to {Uri}", pot.CoverImage);
            }

            var contract = GeneratePotContract();

            pot.Address = contract.Address;
            pot.Mnemonic = contract.Mnemonic;

            db.Insert(pot);
            db.Insert(new QueuePotUpdate { PotId = pot.Id });

            return pot;
        }

        protected (string RawAddress, long Amount, string Payload) PrepareTxInfo(Pot pot, Jetton jetton, string userJettonAddress)
        {
            var rawAddress = TonLibDotNet.Utils.AddressUtils.Instance.MakeRaw(pot.OwnerUserAddress);
            var tonAmount = TonLibDotNet.Utils.CoinUtils.Instance.ToNano(0.1M);

            //var jettonAmount = new BigInteger(pot.InitialSize) * (10 ^ jetton.Decimals);
            //var payload = TonLibDotNet.Recipes.Tep74Jettons.Instance.CreateTransferCell((ulong)pot.Id, jettonAmount, pot.Address, pot.Address, null, 0.001M, null);
            var payload = new CellBuilder().StoreInt(0, 32).StoreString("Test").Build();

            return (rawAddress, tonAmount, payload.ToBoc().SerializeToBase64());
        }

        public record CheckResult(bool Valid, IDictionary<string, string[]>? Errors);

        public record NewPotInfo(string Key, string TxAddress, long TxAmount, string TxPayload);

        public class CreateNewPotException(string message)
            : Exception(message)
        {
            // Nothing
        }
    }
}
