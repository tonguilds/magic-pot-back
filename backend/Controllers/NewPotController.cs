namespace MagicPot.Backend.Controllers
{
    using System.ComponentModel.DataAnnotations;
    using System.Net.Mime;
    using System.Numerics;
    using MagicPot.Backend.Attributes;
    using MagicPot.Backend.Data;
    using MagicPot.Backend.Services.Api;
    using MagicPot.Backend.Utils;
    using Microsoft.AspNetCore.Mvc;
    using SixLabors.ImageSharp;
    using Swashbuckle.AspNetCore.Annotations;
    using TonLibDotNet;

    [ApiController]
    [Produces(MediaTypeNames.Application.Json)]
    [Route("/api/[controller]/[action]")]
    [SwaggerResponse(200, "Request is accepted, processed and response contains requested data.")]
    public class NewPotController(
        Lazy<IDbProvider> lazyDbProvider,
        ILogger<NewPotController> logger,
        IFileService fileService,
        NotificationService notificationService)
        : ControllerBase
    {
        private const int MaxRetries = 100;
        private static readonly Random Rand = new();

        /// <summary>
        /// Checks new pot data (without creating when no errors found).
        /// </summary>
        /// <param name="initData">Value of <see href="https://core.telegram.org/bots/webapps#initializing-mini-apps">initData</see> Telegram property.</param>
        /// <param name="model">Pot parameters.</param>
        /// <returns>Check result: status and list of errors.</returns>
        [HttpPost]
        [Consumes(MediaTypeNames.Application.Json)]
        public async Task<CheckResult> CheckNewPotAsJson(
            [Required(AllowEmptyStrings = false), InitDataValidation, FromHeader(Name = BackendOptions.TelegramInitDataHeaderName)] string initData,
            [Required] NewPotWithCoverModel model)
        {
            if (!ModelState.IsValid)
            {
                return new CheckResult(false, new ValidationProblemDetails(ModelState).Errors);
            }

            await ValidateJetton(model);
            using var ms = await ValidateCoverImage(model.CoverImage, null, nameof(model.CoverImage));

            return ModelState.IsValid ? new CheckResult(true, null) : new CheckResult(false, new ValidationProblemDetails(ModelState).Errors);
        }

        /// <summary>
        /// Checks new pot data (without creating when no errors found).
        /// </summary>
        /// <param name="initData">Value of <see href="https://core.telegram.org/bots/webapps#initializing-mini-apps">initData</see> Telegram property.</param>
        /// <param name="model">Pot parameters.</param>
        /// <param name="coverImage">Pot cover image.</param>
        /// <returns>Check result: status and list of errors.</returns>
        [HttpPost]
        [Consumes(MediaTypeNames.Multipart.FormData)]
        public async Task<CheckResult> CheckNewPotAsForm(
            [Required(AllowEmptyStrings = false), InitDataValidation, FromHeader(Name = BackendOptions.TelegramInitDataHeaderName)] string initData,
            [Required, FromForm] NewPotModel model,
            IFormFile? coverImage)
        {
            if (!ModelState.IsValid)
            {
                return new CheckResult(false, new ValidationProblemDetails(ModelState).Errors);
            }

            await ValidateJetton(model);
            using var ms = await ValidateCoverImage(null, coverImage, nameof(coverImage));

            return ModelState.IsValid ? new CheckResult(true, null) : new CheckResult(false, new ValidationProblemDetails(ModelState).Errors);
        }

        /// <summary>
        /// Checks new pot data, and creates new pot when no errors found.
        /// </summary>
        /// <param name="initData">Value of <see href="https://core.telegram.org/bots/webapps#initializing-mini-apps">initData</see> Telegram property.</param>
        /// <param name="model">Pot parameters.</param>
        /// <returns>Pot string key (for future usage) and transaction data for user to send initial prize.</returns>
        [HttpPost]
        [Consumes(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<NewPotInfo>> CreateNewPotAsJson(
            [Required(AllowEmptyStrings = false), InitDataValidation, FromHeader(Name = BackendOptions.TelegramInitDataHeaderName)] string initData,
            [Required] NewPotWithCoverModel model)
        {
            using var ms = await ValidateCoverImage(model.CoverImage, null, nameof(model.CoverImage));

            return await CreateNewPot(initData, model, ms);
        }

        /// <summary>
        /// Checks new pot data, and creates new pot when no errors found.
        /// </summary>
        /// <param name="initData">Value of <see href="https://core.telegram.org/bots/webapps#initializing-mini-apps">initData</see> Telegram property.</param>
        /// <param name="model">Pot parameters.</param>
        /// <param name="coverImage">Pot cover image.</param>
        /// <returns>Pot string key (for future usage) and transaction data for user to send initial prize.</returns>
        [HttpPost]
        [Consumes(MediaTypeNames.Multipart.FormData)]
        public async Task<ActionResult<NewPotInfo>> CreateNewPotAsForm(
            [Required(AllowEmptyStrings = false), InitDataValidation, FromHeader(Name = BackendOptions.TelegramInitDataHeaderName)] string initData,
            [Required, FromForm] NewPotModel model,
            IFormFile? coverImage)
        {
            using var ms = await ValidateCoverImage(null, coverImage, nameof(coverImage));

            return await CreateNewPot(initData, model, ms);
        }

        /// <summary>
        /// Re-creates transaction data for sending prize to pot (returns same result as CreateNewPotXxx, but for existing pot).
        /// </summary>
        /// <param name="initData">Value of <see href="https://core.telegram.org/bots/webapps#initializing-mini-apps">initData</see> Telegram property.</param>
        /// <param name="key">Pot key.</param>
        /// <returns>Pot string key (for future usage) and transaction data for user to send initial prize.</returns>
        [HttpPost]
        [Consumes(MediaTypeNames.Application.Json)]
        public ActionResult<NewPotInfo> GetSendPrizeTransactionData(
            [Required(AllowEmptyStrings = false), InitDataValidation, FromHeader(Name = BackendOptions.TelegramInitDataHeaderName)] string initData,
            [Required(AllowEmptyStrings = false)] string key)
        {
            if (!ModelState.IsValid)
            {
                return new BadRequestObjectResult(new ValidationProblemDetails(ModelState));
            }

            var tgUser = InitDataValidationAttribute.GetUserDataWithoutValidation(initData);
            var db = lazyDbProvider.Value.MainDb;
            var pot = db.Find<Pot>(x => x.OwnerUserId == tgUser.Id && x.Key == key);
            if (pot == null)
            {
                return NotFound();
            }

            var jetton = db.Get<Jetton>(x => x.Address == pot.JettonMaster);
            var ujw = db.Get<UserJettonWallet>(x => x.MainWallet == pot.OwnerUserAddress && x.JettonMaster == pot.Address);
            var txInfo = PrepareTxInfo(pot, jetton, ujw.JettonWallet!);

            return new NewPotInfo(pot.Key, txInfo.RawAddress, txInfo.Amount, txInfo.Payload);
        }

        /// <summary>
        /// [Re]activates watching for new transactions to Pot address (after successful sending tx from TON Connect).
        /// </summary>
        /// <param name="initData">Value of <see href="https://core.telegram.org/bots/webapps#initializing-mini-apps">initData</see> Telegram property.</param>
        /// <param name="key">Pot key.</param>
        /// <param name="boc">BOC from TON Connect after successful transaction execution.</param>
        [HttpPost]
        [Consumes(MediaTypeNames.Application.Json)]
        public ActionResult WaitForPrizeTransaction(
            [Required(AllowEmptyStrings = false), InitDataValidation, FromHeader(Name = BackendOptions.TelegramInitDataHeaderName)] string initData,
            [Required(AllowEmptyStrings = false)] string key,
            [Required(AllowEmptyStrings = false)] string boc)
        {
            if (!ModelState.IsValid)
            {
                return new BadRequestObjectResult(new ValidationProblemDetails(ModelState));
            }

            var tgUser = InitDataValidationAttribute.GetUserDataWithoutValidation(initData);
            var db = lazyDbProvider.Value.MainDb;
            var pot = db.Find<Pot>(x => x.OwnerUserId == tgUser.Id && x.Key == key);
            if (pot == null)
            {
                return NotFound();
            }

            pot.Touch();
            db.Update(pot);

            notificationService.TryRun<Services.Indexer.PotUpdateTask>();

            return Ok();
        }

        protected async Task<ActionResult<NewPotInfo>> CreateNewPot(string initData, NewPotModel model, MemoryStream? coverImage)
        {
            if (!ModelState.IsValid)
            {
                return new BadRequestObjectResult(new ValidationProblemDetails(ModelState));
            }

            var (jetton, userJettonAddress) = await ValidateJetton(model);

            if (!ModelState.IsValid)
            {
                return new BadRequestObjectResult(new ValidationProblemDetails(ModelState));
            }

            var tgUser = InitDataValidationAttribute.GetUserDataWithoutValidation(initData);

            var user = lazyDbProvider.Value.GetOrCreateUser(tgUser);

            var pot = await CreatePot(model, user.Id, jetton!.Address, coverImage);
            var txInfo = PrepareTxInfo(pot, jetton, userJettonAddress!);

            return new NewPotInfo(pot.Key, txInfo.RawAddress, txInfo.Amount, txInfo.Payload);
        }

        protected async Task<(Jetton? Jetton, string? UserJettonWallet)> ValidateJetton(NewPotModel model)
        {
            if (string.IsNullOrWhiteSpace(model.TokenAddress))
            {
                return (null, null);
            }

            model.TokenAddress = AddressConverter.ToContract(model.TokenAddress);

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

                notificationService.TryRun<CachedData>();
            }

            if (string.IsNullOrWhiteSpace(model.UserAddress))
            {
                return (null, null);
            }

            model.UserAddress = AddressConverter.ToUser(model.UserAddress);

            var ujw = db.Find<UserJettonWallet>(x => x.MainWallet == model.UserAddress && x.JettonMaster == jetton.Address);
            if (ujw == null)
            {
                ujw = new UserJettonWallet()
                {
                    MainWallet = model.UserAddress,
                    JettonMaster = jetton.Address,
                };

                db.Insert(ujw);

                notificationService.TryRun<Services.Indexer.DetectUserJettonAddressesTask>();
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
                var count = db.Table<Pot>().Count(x => x.Id == id);
                if (count == 0)
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

        protected async Task<Pot> CreatePot(NewPotModel model, long userId, string tokenAddress, MemoryStream? coverImage)
        {
            var db = lazyDbProvider.Value.MainDb;

            var pot = new Pot
            {
                Id = GeneratePotId(),
                Name = model.Name,
                OwnerUserId = userId,
                OwnerUserAddress = model.UserAddress,
                JettonMaster = tokenAddress,
                JettonWallet = string.Empty,
                InitialSize = model.InitialSize,
                TotalSize = 0,
                State = PotState.Created,
                Created = DateTimeOffset.UtcNow,
                Countdown = TimeSpan.FromMinutes(model.CountdownTimerMinutes),
                TxSizeNext = model.TransactionSize,
                TxSizeIncrease = model.IncreasingTransactionPercentage,
                CreatorPercent = model.CreatorPercent ?? 0,
                LastTxPercent = model.LastTransactionsPercent ?? 0,
                LastTxCount = model.LastTransactionsCount ?? 0,
                RandomTxPercent = model.RandomTransactionsPercent ?? 0,
                RandomTxCount = model.RandomTransactionsCount ?? 0,
                ReferralsPercent = model.ReferralsPercent ?? 0,
                BurnPercent = model.BurnPercent ?? 0,
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

            pot.Touch();

            db.Insert(pot);

            notificationService.TryRun<CachedData>();
            notificationService.TryRun<BackupTask>();
            notificationService.TryRun<Services.Indexer.PotUpdateTask>();
            notificationService.TryRun<Services.Indexer.PrecacheMnemonicsTask>();

            return pot;
        }

        protected (string RawAddress, long Amount, string Payload) PrepareTxInfo(Pot pot, Jetton jetton, string userJettonAddress)
        {
            var rawAddress = AddressConverter.ToRaw(userJettonAddress);
            var tonAmount = TonLibDotNet.Utils.CoinUtils.Instance.ToNano(0.5M + 0.2M);
            var jettonAmount = (BigInteger)pot.InitialSize * (BigInteger)Math.Pow(10, jetton.Decimals);
            var payload = TonLibDotNet.Recipes.Tep74Jettons.Instance.CreateTransferCell(
                (ulong)pot.Id,
                jettonAmount,
                pot.OwnerUserAddress,
                pot.Address,
                null,
                0.02M,
                null);

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
