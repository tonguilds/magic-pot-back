namespace MagicPot.Backend.Controllers
{
    using System.ComponentModel.DataAnnotations;
    using System.Net.Mime;
    using System.Numerics;
    using System.Reflection;
    using MagicPot.Backend.Attributes;
    using MagicPot.Backend.Data;
    using MagicPot.Backend.Models;
    using MagicPot.Backend.Services;
    using MagicPot.Backend.Services.Api;
    using MagicPot.Backend.Utils;
    using Microsoft.AspNetCore.Mvc;
    using Swashbuckle.AspNetCore.Annotations;
    using TonLibDotNet;
    using TonLibDotNet.Cells;

    [ApiController]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [Route("/api/[controller]/[action]")]
    [SwaggerResponse(200, "Request is accepted, processed and response contains requested data.")]
    public class PotController(CachedData cachedData, Lazy<IDbProvider> lazyDbProvider, INotificationService notificationService) : ControllerBase
    {
        /// <summary>
        /// Returns Pot in any state, but only when owned by specified user.
        /// </summary>
        /// <param name="initData">Value of <see href="https://core.telegram.org/bots/webapps#initializing-mini-apps">initData</see> Telegram property.</param>
        /// <param name="key">Pot key.</param>
        /// <returns><see cref="PotInfo"/> data when found.</returns>
        /// <remarks>See <see cref="GetPot"/> for status flags description.</remarks>
        [SwaggerResponse(404, "Pot with specified key does not exist or not owned by specified user.")]
        [HttpGet("{key:minlength(3)}")]
        public ActionResult<PotInfo> GetMyPot(
            [Required(AllowEmptyStrings = false), InitDataValidation, FromHeader(Name = BackendOptions.TelegramInitDataHeaderName)] string initData,
            [Required(AllowEmptyStrings = false)] string key)
        {
            if (!ModelState.IsValid)
            {
                return new BadRequestObjectResult(new ValidationProblemDetails(ModelState));
            }

            if (!cachedData.AllPotKeys.TryGetValue(key, out key))
            {
                return NotFound();
            }

            var tgUser = InitDataValidationAttribute.GetUserDataWithoutValidation(initData);
            lazyDbProvider.Value.GetOrCreateUser(tgUser);

            var db = lazyDbProvider.Value.MainDb;

            var pot = db.Get<Pot>(x => x.Key == key);
            if (pot.OwnerUserId != tgUser.Id)
            {
                return NotFound();
            }

            var jetton = cachedData.AllJettons[pot.JettonMaster];
            var creator = db.Get<User>(pot.OwnerUserId);
            cachedData.ActivePotTransactions.TryGetValue(pot.Id, out var txlist);

            return PotInfo.Create(pot, jetton, creator, txlist, cachedData.ActivePotUsers);
        }

        /// <summary>
        /// Returns Pot in active state.
        /// </summary>
        /// <param name="key">Pot key.</param>
        /// <returns><see cref="PotInfo"/> data when found.</returns>
        /// <remarks>
        /// Meaning of status flags (in recommended check order):
        ///
        /// * <b>IsWaitingForPrizeTransacton == false</b>: Pot has been created, but prize pool tokens have not [yet] arrived; only Creator can see this pot.
        /// * <b>IsStarted == false</b>: Pot has been funded, visible to users and waiting for first user bet/transaction. Timer is <b>NOT</b> yet started (<b>EndsAt</b> is null).
        /// * <b>IsEnded == false</b>: Timer is ticking (<b>EndsAt</b> is not null), Pot can accept more bets/transactions.
        /// </remarks>
        [SwaggerResponse(404, "Pot with specified key does not exist or not active.")]
        [HttpGet("{key:minlength(3)}")]
        public ActionResult<PotInfo> GetPot([Required(AllowEmptyStrings = false)] string key)
        {
            if (!ModelState.IsValid)
            {
                return new BadRequestObjectResult(new ValidationProblemDetails(ModelState));
            }

            if (!cachedData.ActivePots.TryGetValue(key, out var pot))
            {
                return NotFound();
            }

            var jetton = cachedData.AllJettons[pot.JettonMaster];
            var creator = cachedData.ActivePotOwners[pot.OwnerUserId];
            cachedData.ActivePotTransactions.TryGetValue(pot.Id, out var txlist);

            return PotInfo.Create(pot, jetton, creator, txlist, cachedData.ActivePotUsers);
        }

        /// <summary>
        /// Returns transaction data for making a bid to selected Pot.
        /// </summary>
        /// <param name="initData">Value of <see href="https://core.telegram.org/bots/webapps#initializing-mini-apps">initData</see> Telegram property.</param>
        /// <param name="model">Request parameters.</param>
        /// <returns><see cref="CreateTransactionResult"/> object with TON Connect data.</returns>
        [SwaggerResponse(404, "Pot with specified key does not exist or not active.")]
        [HttpPost]
        public ActionResult<CreateTransactionResult> CreateTransaction(
            [Required(AllowEmptyStrings = false), InitDataValidation, FromHeader(Name = BackendOptions.TelegramInitDataHeaderName)] string initData,
            [Required] CreateTransactionModel model)
        {
            if (!ModelState.IsValid)
            {
                return new CreateTransactionResult() { Errors = new ValidationProblemDetails(ModelState).Errors };
            }

            if (!cachedData.ActivePots.TryGetValue(model.Key, out var pot))
            {
                return NotFound();
            }

            if (model.Amount.HasValue && model.Amount.Value < pot.TxSizeNext)
            {
                ModelState.AddModelError(nameof(model.Amount), Messages.TxAmountIsLessThanRequired);
            }

            if (!ModelState.IsValid)
            {
                return new CreateTransactionResult() { Errors = new ValidationProblemDetails(ModelState).Errors };
            }

            var tgUser = InitDataValidationAttribute.GetUserDataWithoutValidation(initData);
            lazyDbProvider.Value.GetOrCreateUser(tgUser);

            var db = lazyDbProvider.Value.MainDb;

            var ujw = db.Find<UserJettonWallet>(x => x.MainWallet == model.UserAddress && x.JettonMaster == pot.JettonMaster);
            if (ujw == null)
            {
                ujw = new UserJettonWallet()
                {
                    MainWallet = model.UserAddress,
                    JettonMaster = pot.JettonMaster,
                };

                db.Insert(ujw);
            }

            if (string.IsNullOrWhiteSpace(ujw.JettonWallet))
            {
                notificationService.TryRun<Services.Indexer.DetectUserJettonAddressesTask>();
                return new CreateTransactionResult() { IsValidatingWallet = true };
            }

            var jetton = cachedData.AllJettons[pot.JettonMaster];

            var (txAmount, txPayload) = PrepareTxInfo(pot, jetton, model.UserAddress, model.Amount ?? pot.TxSizeNext, tgUser.Id, model.Referrer);

            return new CreateTransactionResult() { TransactionInfo = new(ujw.JettonWallet!, txAmount, txPayload) };
        }

        /// <summary>
        /// [Re]activates watching for new transactions to Pot address (after successful sending tx from TON Connect).
        /// </summary>
        /// <param name="initData">Value of <see href="https://core.telegram.org/bots/webapps#initializing-mini-apps">initData</see> Telegram property.</param>
        /// <param name="key">Pot key.</param>
        /// <param name="boc">BOC from TON Connect after successful transaction execution.</param>
        [HttpPost("{key:minlength(3)}")]
        [Consumes(MediaTypeNames.Text.Plain)]
        [SwaggerResponse(404, "Pot with specified key does not exist or not active.")]
        public ActionResult WaitForTransaction(
            [Required(AllowEmptyStrings = false), InitDataValidation, FromHeader(Name = BackendOptions.TelegramInitDataHeaderName)] string initData,
            [Required(AllowEmptyStrings = false)] string key,
            [Required(AllowEmptyStrings = false), FromBody] string boc)
        {
            if (!ModelState.IsValid)
            {
                return new BadRequestObjectResult(new ValidationProblemDetails(ModelState));
            }

            if (!cachedData.ActivePots.TryGetValue(key, out var pot))
            {
                return NotFound();
            }

            var tgUser = InitDataValidationAttribute.GetUserDataWithoutValidation(initData);
            lazyDbProvider.Value.GetOrCreateUser(tgUser);

            // re-read pot from DB
            pot = lazyDbProvider.Value.MainDb.Get<Pot>(pot.Id);
            pot.Touch();
            lazyDbProvider.Value.MainDb.Update(pot);

            notificationService.TryRun<Services.Indexer.PotUpdateTask>();

            return Ok();
        }

        /// <summary>
        /// Sends rich-formatted message with Pot description to user in Telegram.
        /// </summary>
        /// <param name="initData">Value of <see href="https://core.telegram.org/bots/webapps#initializing-mini-apps">initData</see> Telegram property.</param>
        /// <param name="key">Pot key.</param>
        /// <param name="userAddress">Referrer (current user) address.</param>
        [HttpPost("{key:minlength(3)}")]
        [Consumes(MediaTypeNames.Application.Json, MediaTypeNames.Text.Plain, MediaTypeNames.Application.FormUrlEncoded)]
        [SwaggerResponse(404, "Pot with specified key does not exist or not active.")]
        [SwaggerResponse(409, "User not allowed bot to write to PM.")]
        public ActionResult SendPromoMessage(
            [Required(AllowEmptyStrings = false), InitDataValidation, FromHeader(Name = BackendOptions.TelegramInitDataHeaderName)] string initData,
            [Required(AllowEmptyStrings = false)] string key,
            [TonAddress] string? userAddress)
        {
            if (!ModelState.IsValid)
            {
                return new BadRequestObjectResult(new ValidationProblemDetails(ModelState));
            }

            if (!cachedData.ActivePots.TryGetValue(key, out var pot))
            {
                return NotFound();
            }

            var tgUser = InitDataValidationAttribute.GetUserDataWithoutValidation(initData);
            lazyDbProvider.Value.GetOrCreateUser(tgUser);

            if (!tgUser.AllowsWriteToPM)
            {
                return Conflict("User not allowed bot to write to PM.");
            }

            lazyDbProvider.Value.GetOrCreateUser(tgUser);

            lazyDbProvider.Value.MainDb.Insert(ScheduledMessage.Create(pot.Id, ScheduledMessageType.ReferralRichMessage, tgUser.Id, userAddress));
            notificationService.TryRun<ScheduledMessageSender>();

            return Ok();
        }

        /// <summary>
        /// Returns list of all Pots, owned (created) by specified user, in any state, ordered by time of creation (in descending order, young pots first).
        /// </summary>
        /// <param name="initData">Value of <see href="https://core.telegram.org/bots/webapps#initializing-mini-apps">initData</see> Telegram property.</param>
        /// <returns>List of <see cref="PotInfo"/> data.</returns>
        /// <remarks>See <see cref="GetPot"/> for status flags description.</remarks>
        [HttpGet]
        public ActionResult<List<PotInfo>> GetCreatedPots([Required(AllowEmptyStrings = false), InitDataValidation, FromHeader(Name = BackendOptions.TelegramInitDataHeaderName)] string initData)
        {
            if (!ModelState.IsValid)
            {
                return new BadRequestObjectResult(new ValidationProblemDetails(ModelState));
            }

            var tgUser = InitDataValidationAttribute.GetUserDataWithoutValidation(initData);
            lazyDbProvider.Value.GetOrCreateUser(tgUser);

            var db = lazyDbProvider.Value.MainDb;

            var pots = db.Table<Pot>().Where(x => x.OwnerUserId == tgUser.Id).OrderByDescending(x => x.Created).ToList();

            var creator = db.Get<User>(tgUser.Id);

            return pots.Select(x =>
                {
                    var jetton = cachedData.AllJettons[x.JettonMaster];
                    cachedData.ActivePotTransactions.TryGetValue(x.Id, out var list);
                    return PotInfo.Create(x, jetton, creator, list, cachedData.ActivePotUsers);
                })
                .ToList();
        }

        /// <summary>
        /// Returns list of active Pots, which current user participated in, ordered by end time (in descending order, nearest ending first).
        /// </summary>
        /// <param name="initData">Value of <see href="https://core.telegram.org/bots/webapps#initializing-mini-apps">initData</see> Telegram property.</param>
        /// <returns>List of <see cref="PotInfo"/> data.</returns>
        /// <remarks>See <see cref="GetPot"/> for status flags description.</remarks>
        [HttpGet]
        public ActionResult<List<PotInfo>> GetParticipatingPots([Required(AllowEmptyStrings = false), InitDataValidation, FromHeader(Name = BackendOptions.TelegramInitDataHeaderName)] string initData)
        {
            if (!ModelState.IsValid)
            {
                return new BadRequestObjectResult(new ValidationProblemDetails(ModelState));
            }

            var tgUser = InitDataValidationAttribute.GetUserDataWithoutValidation(initData);
            lazyDbProvider.Value.GetOrCreateUser(tgUser);

            var db = lazyDbProvider.Value.MainDb;

            var potIds = db.Table<Transaction>().Where(x => x.UserId == tgUser.Id).Select(x => x.PotId).Distinct().ToHashSet();

            var now = DateTimeOffset.UtcNow;
            return cachedData.ActivePots.Values
                .Where(x => potIds.Contains(x.Id))
                .Select(x =>
                {
                    var jetton = cachedData.AllJettons[x.JettonMaster];
                    var creator = cachedData.ActivePotOwners[x.OwnerUserId];
                    cachedData.ActivePotTransactions.TryGetValue(x.Id, out var list);
                    return PotInfo.Create(x, jetton, creator, list, cachedData.ActivePotUsers);
                })
                .OrderBy(x => x.IsEnded)
                .ThenBy(x => Math.Abs(now.Subtract(x.EndsAt ?? now).Ticks))
                .ToList();
        }

        protected (long Amount, string Payload) PrepareTxInfo(Pot pot, Jetton jetton, string userAddress, decimal amount, long currentUserId, string? referrerAddress)
        {
            var tonAmount = TonLibDotNet.Utils.CoinUtils.Instance.ToNano(cachedData.Options.TonAmountForGas + cachedData.Options.TonAmountForInterest);
            var jettonAmount = (BigInteger)(amount * (decimal)Math.Pow(10, jetton.Decimals));
            var forwardPayload = PayloadEncoder.EncodeBet(currentUserId, referrerAddress);
            var payload = TonLibDotNet.Recipes.Tep74Jettons.Instance.CreateTransferCell(
                (ulong)pot.Id,
                jettonAmount,
                pot.Address,
                userAddress,
                null,
                cachedData.Options.TonAmountForInterest,
                forwardPayload);

            return (tonAmount, payload.ToBoc().SerializeToBase64());
        }
    }
}
