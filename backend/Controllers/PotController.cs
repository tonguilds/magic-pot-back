namespace MagicPot.Backend.Controllers
{
    using System.ComponentModel.DataAnnotations;
    using System.Net.Mime;
    using MagicPot.Backend.Attributes;
    using MagicPot.Backend.Data;
    using MagicPot.Backend.Services.Api;
    using Microsoft.AspNetCore.Mvc;
    using Swashbuckle.AspNetCore.Annotations;

    [ApiController]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [Route("/api/[controller]/[action]")]
    [SwaggerResponse(200, "Request is accepted, processed and response contains requested data.")]
    public class PotController(CachedData cachedData, Lazy<IDbProvider> lazyDbProvider) : ControllerBase
    {
        /// <summary>
        /// Returns Pot in any state, but only when owned by specified user.
        /// </summary>
        /// <param name="initData">Value of <see href="https://core.telegram.org/bots/webapps#initializing-mini-apps">initData</see> Telegram property.</param>
        /// <param name="key">Pot key.</param>
        /// <returns><see cref="PotInfo"/> data when found.</returns>
        [SwaggerResponse(404, "Pot with specified key does not exist, or not owned by specified user.")]
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

            var db = lazyDbProvider.Value.MainDb;

            var pot = db.Get<Pot>(x => x.Key == key);
            if (pot.OwnerUserId != tgUser.Id)
            {
                return NotFound();
            }

            var jetton = cachedData.AllJettons[pot.JettonMaster];
            var creator = cachedData.ActivePotOwners[pot.OwnerUserId];

            return PotInfo.Create(pot, jetton, creator);
        }

        /// <summary>
        /// Returns Pot in active state.
        /// </summary>
        /// <param name="key">Pot key.</param>
        /// <returns><see cref="PotInfo"/> data when found.</returns>
        [SwaggerResponse(404, "Pot with specified key does not exist, or not active.")]
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

            return PotInfo.Create(pot, jetton, creator);
        }
    }
}
