namespace MagicPot.Backend.Controllers
{
    using System.ComponentModel.DataAnnotations;
    using System.Net.Mime;
    using System.Numerics;
    using MagicPot.Backend.Attributes;
    using MagicPot.Backend.Data;
    using MagicPot.Backend.Models;
    using MagicPot.Backend.Services;
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
        Lazy<IFileService> fileService,
        Lazy<ITonApiService> tonApiService,
        CachedData cachedData,
        INotificationService notificationService)
        : ControllerBase
    {
        private const int MaxRetries = 100;
        private static readonly Random Rand = new();

        private static readonly SixLabors.ImageSharp.Formats.DecoderOptions ImageSharpDecoderOptions = new()
        {
            Configuration = new(
                new SixLabors.ImageSharp.Formats.Jpeg.JpegConfigurationModule(),
                new SixLabors.ImageSharp.Formats.Png.PngConfigurationModule(),
                new SixLabors.ImageSharp.Formats.Webp.WebpConfigurationModule(),
                new SixLabors.ImageSharp.Formats.Gif.GifConfigurationModule()),
        };

        /// <summary>
        /// Checks new pot data (without creating when no errors found).
        /// </summary>
        /// <param name="initData">Value of <see href="https://core.telegram.org/bots/webapps#initializing-mini-apps">initData</see> Telegram property.</param>
        /// <param name="model">Pot parameters.</param>
        /// <returns>Check result: status and list of errors.</returns>
        [HttpPost]
        [Consumes(MediaTypeNames.Application.Json)]
        public async Task<CheckPotResult> CheckNewPotAsJson(
            [Required(AllowEmptyStrings = false), InitDataValidation, FromHeader(Name = BackendOptions.TelegramInitDataHeaderName)] string initData,
            [Required] NewPotWithCoverModel model)
        {
            if (!ModelState.IsValid)
            {
                return new CheckPotResult() { Errors = new ValidationProblemDetails(ModelState).Errors };
            }

            var jetton = await ValidateJetton(model);
            if (jetton == null)
            {
                return new CheckPotResult() { Errors = new ValidationProblemDetails(ModelState).Errors };
            }

            var ujw = ValidateUserJettonWallet(model);

            (var ms, _) = await ValidateCoverImage(model.CoverImage, null, nameof(model.CoverImage));
            ms?.Dispose();

            return new CheckPotResult()
            {
                Errors = ModelState.IsValid ? null : new ValidationProblemDetails(ModelState).Errors,
                IsValidatingWallet = string.IsNullOrEmpty(ujw),
            };
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
        public async Task<CheckPotResult> CheckNewPotAsForm(
            [Required(AllowEmptyStrings = false), InitDataValidation, FromHeader(Name = BackendOptions.TelegramInitDataHeaderName)] string initData,
            [Required, FromForm] NewPotModel model,
            IFormFile? coverImage)
        {
            if (!ModelState.IsValid)
            {
                return new CheckPotResult() { Errors = new ValidationProblemDetails(ModelState).Errors };
            }

            var jetton = await ValidateJetton(model);
            if (jetton == null)
            {
                return new CheckPotResult() { Errors = new ValidationProblemDetails(ModelState).Errors };
            }

            var ujw = ValidateUserJettonWallet(model);

            (var ms, _) = await ValidateCoverImage(null, coverImage, nameof(coverImage));
            ms?.Dispose();

            return new CheckPotResult()
            {
                Errors = ModelState.IsValid ? null : new ValidationProblemDetails(ModelState).Errors,
                IsValidatingWallet = string.IsNullOrEmpty(ujw),
            };
        }

        /// <summary>
        /// Checks new pot data, and creates new pot when no errors found.
        /// </summary>
        /// <param name="initData">Value of <see href="https://core.telegram.org/bots/webapps#initializing-mini-apps">initData</see> Telegram property.</param>
        /// <param name="model">Pot parameters.</param>
        /// <returns>Pot string key (for future usage) and transaction data for user to send initial prize.</returns>
        [HttpPost]
        [Consumes(MediaTypeNames.Application.Json)]
        public async Task<CreatePotResult> CreateNewPotAsJson(
            [Required(AllowEmptyStrings = false), InitDataValidation, FromHeader(Name = BackendOptions.TelegramInitDataHeaderName)] string initData,
            [Required] NewPotWithCoverModel model)
        {
            var (ms, animated) = await ValidateCoverImage(model.CoverImage, null, nameof(model.CoverImage));

            return await CreateNewPot(initData, model, ms, animated);
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
        public async Task<CreatePotResult> CreateNewPotAsForm(
            [Required(AllowEmptyStrings = false), InitDataValidation, FromHeader(Name = BackendOptions.TelegramInitDataHeaderName)] string initData,
            [Required, FromForm] NewPotModel model,
            IFormFile? coverImage)
        {
            var (ms, animated) = await ValidateCoverImage(null, coverImage, nameof(coverImage));

            return await CreateNewPot(initData, model, ms, animated);
        }

        /// <summary>
        /// Re-creates transaction data for sending prize to pot (returns same result as CreateNewPotXxx, but for existing pot).
        /// </summary>
        /// <param name="initData">Value of <see href="https://core.telegram.org/bots/webapps#initializing-mini-apps">initData</see> Telegram property.</param>
        /// <param name="key">Pot key.</param>
        /// <returns>Pot string key and transaction data for user to send initial prize.</returns>
        /// <remarks>User must be an owner of requested pot, otherwise error 404 will be returned.</remarks>
        [HttpGet("{key:minlength(3)}")]
        [SwaggerResponse(404, "Pot with specified key not found or not owned by specified user.")]
        public ActionResult<TonConnectTransactionInfo> GetSendPrizeTransactionData(
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
            var ujw = db.Get<UserJettonWallet>(x => x.MainWallet == pot.OwnerUserAddress && x.JettonMaster == pot.JettonMaster);
            var (txAmount, txPayload) = PrepareTxInfo(pot, jetton, tgUser.Id);

            return new TonConnectTransactionInfo(ujw.JettonWallet!, txAmount, txPayload);
        }

        /// <summary>
        /// [Re]activates watching for new transactions to Pot address (after successful sending tx from TON Connect).
        /// </summary>
        /// <param name="initData">Value of <see href="https://core.telegram.org/bots/webapps#initializing-mini-apps">initData</see> Telegram property.</param>
        /// <param name="key">Pot key.</param>
        /// <param name="boc">BOC from TON Connect after successful transaction execution.</param>
        /// <remarks>User must be an owner of requested pot, otherwise error 404 will be returned.</remarks>
        [HttpPost("{key:minlength(3)}")]
        [Consumes(MediaTypeNames.Text.Plain)]
        [SwaggerResponse(404, "Pot with specified key not found or not owned by specified user.")]
        public ActionResult WaitForPrizeTransaction(
            [Required(AllowEmptyStrings = false), InitDataValidation, FromHeader(Name = BackendOptions.TelegramInitDataHeaderName)] string initData,
            [Required(AllowEmptyStrings = false)] string key,
            [Required(AllowEmptyStrings = false), FromBody] string boc)
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

        protected async Task<CreatePotResult> CreateNewPot(string initData, NewPotModel model, MemoryStream? coverImage, bool coverImageAnimated)
        {
            if (!ModelState.IsValid)
            {
                return new CreatePotResult() { Errors = new ValidationProblemDetails(ModelState).Errors };
            }

            var jetton = await ValidateJetton(model);
            if (jetton == null)
            {
                return new CreatePotResult() { Errors = new ValidationProblemDetails(ModelState).Errors };
            }

            var ujw = ValidateUserJettonWallet(model);
            if (string.IsNullOrEmpty(ujw))
            {
                return new CreatePotResult() { IsValidatingWallet = true };
            }

            if (!ModelState.IsValid)
            {
                return new CreatePotResult() { Errors = new ValidationProblemDetails(ModelState).Errors };
            }

            var tgUser = InitDataValidationAttribute.GetUserDataWithoutValidation(initData);

            var user = lazyDbProvider.Value.GetOrCreateUser(tgUser);

            var pot = await CreatePot(model, user.Id, jetton!.Address, coverImage, coverImageAnimated);
            var (txAmount, txPayload) = PrepareTxInfo(pot, jetton, pot.OwnerUserId);

            logger.LogInformation("New Pot created: {Key} by #{UserId} / @{User} for {Amount} {Symbol}", pot.Key, user.Id, user.Username, pot.InitialSize, jetton.Symbol);

            return new CreatePotResult() { Key = pot.Key, TransactionInfo = new(ujw!, txAmount, txPayload) };
        }

        protected async Task<Jetton?> ValidateJetton(NewPotModel model)
        {
            if (string.IsNullOrWhiteSpace(model.TokenAddress))
            {
                return null;
            }

            model.TokenAddress = AddressConverter.ToContract(model.TokenAddress);

            var db = lazyDbProvider.Value.MainDb;

            var jetton = db.Find<Jetton>(x => x.Address == model.TokenAddress);
            if (jetton == null)
            {
                jetton = await tonApiService.Value.GetJettonInfo(model.TokenAddress);
                if (jetton == null)
                {
                    ModelState.AddModelError(nameof(model.TokenAddress), Messages.AddressIsNotAJetton);
                    return null;
                }

                var existing = db.Find<Jetton>(x => x.Address == jetton.Address);
                if (existing == null)
                {
                    db.Insert(jetton);
                    logger.LogInformation("New Jetton saved: {Symbox} {Address} ({Name})", jetton.Symbol, jetton.Address, jetton.Name);
                }

                notificationService.TryRun<CachedData>();
            }

            return jetton;
        }

        protected string? ValidateUserJettonWallet(NewPotModel model)
        {
            if (string.IsNullOrWhiteSpace(model.UserAddress))
            {
                return null;
            }

            model.UserAddress = AddressConverter.ToUser(model.UserAddress);

            var db = lazyDbProvider.Value.MainDb;

            var ujw = db.Find<UserJettonWallet>(x => x.MainWallet == model.UserAddress && x.JettonMaster == model.TokenAddress);
            if (ujw == null)
            {
                ujw = new UserJettonWallet()
                {
                    MainWallet = model.UserAddress,
                    JettonMaster = model.TokenAddress,
                };

                db.Insert(ujw);
            }

            var boundary = DateTimeOffset.UtcNow.Subtract(cachedData.Options.JettonBalanceValidity);
            if (ujw.Balance is null || ujw.Updated < boundary)
            {
                ujw.JettonWallet = null;
                db.Update(ujw);
            }

            if (string.IsNullOrWhiteSpace(ujw.JettonWallet))
            {
                notificationService.TryRun<Services.Indexer.DetectUserJettonAddressesTask>();
                return null;
            }

            if (ujw.Balance < model.InitialSize)
            {
                ModelState.AddModelError(nameof(model.InitialSize), Messages.UnsufficientJettonAmount);
            }

            return ujw.JettonWallet;
        }

        protected async Task<(MemoryStream? Image, bool Animated)> ValidateCoverImage(string? dataUrl, IFormFile? file, string paramName)
        {
            MemoryStream? image = null;

            //// Sample: data:image/jpeg;base64,...
            if (!string.IsNullOrEmpty(dataUrl))
            {
                var pos = dataUrl.IndexOf(',') + 1;
                if (pos == 0)
                {
                    ModelState.AddModelError(paramName, "Unknown image data format");
                    return (null, false);
                }

                try
                {
                    var bytes = Convert.FromBase64String(dataUrl[pos..]);
                    image = new MemoryStream(bytes);
                }
                catch (FormatException)
                {
                    ModelState.AddModelError(paramName, "Unable to decode image data");
                    return (null, false);
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
                return (null, false);
            }

            try
            {
                var img = await Image.IdentifyAsync(ImageSharpDecoderOptions, image);
                var frames = img.FrameMetadataCollection.Count;
                logger.LogDebug("Image OK: {Format}, width {Width}, height {Height}, frames {Count}", img.Metadata.DecodedImageFormat?.Name, img.Width, img.Height, frames);
                image.Position = 0;
                return (image, frames > 1);
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Image not Ok");
                ModelState.AddModelError(paramName, "Not a valid image, or format not supported");
                return (null, false);
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

        protected async Task<Pot> CreatePot(NewPotModel model, long userId, string tokenAddress, MemoryStream? coverImage, bool coverImageAnimated)
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
                Created = DateTimeOffset.UtcNow,
                Countdown = TimeSpan.FromMinutes(model.CountdownTimerMinutes),
                TxSizeNext = model.TransactionSize,
                TxSizeIncrease = model.IncreasingTransactionPercentage ?? 0,
                CreatorPercent = model.CreatorPercent ?? 0,
                LastTxPercent = model.LastTransactionsPercent ?? 0,
                LastTxCount = model.LastTransactionsCount ?? 0,
                RandomTxPercent = model.RandomTransactionsPercent ?? 0,
                RandomTxCount = model.RandomTransactionsCount ?? 0,
                ReferrersPercent = model.ReferrersPercent ?? 0,
                BurnPercent = model.BurnPercent ?? 0,
            };

            pot.Key = Base36.Encode(pot.Id);

            if (coverImage != null)
            {
                pot.CoverImage = await fileService.Value.Upload(coverImage, pot.Key + ".dat");
                pot.CoverIsAnimated = coverImageAnimated;
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

        protected (long Amount, string Payload) PrepareTxInfo(Pot pot, Jetton jetton, long currentUserId)
        {
            var tonAmount = TonLibDotNet.Utils.CoinUtils.Instance.ToNano(cachedData.Options.TonAmountForGas + cachedData.Options.TonAmountForInterest);
            var jettonAmount = (BigInteger)pot.InitialSize * (BigInteger)Math.Pow(10, jetton.Decimals);
            var forwardPayload = PayloadEncoder.EncodePrize(currentUserId);
            var payload = TonLibDotNet.Recipes.Tep74Jettons.Instance.CreateTransferCell(
                (ulong)pot.Id,
                jettonAmount,
                pot.Address,
                pot.OwnerUserAddress,
                null,
                cachedData.Options.TonAmountForInterest,
                forwardPayload);

            return (tonAmount, payload.ToBoc().SerializeToBase64());
        }

        public class CreateNewPotException(string message)
            : Exception(message)
        {
            // Nothing
        }
    }
}
