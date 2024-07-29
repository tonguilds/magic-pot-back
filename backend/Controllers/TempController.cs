namespace MagicPot.Backend.Controllers
{
    using System.ComponentModel.DataAnnotations;
    using System.Net.Mime;
    using MagicPot.Backend.Attributes;
    using MagicPot.Backend.Data;
    using MagicPot.Backend.Models;
    using MagicPot.Backend.Services.Api;
    using Microsoft.AspNetCore.Mvc;
    using Swashbuckle.AspNetCore.Annotations;

    [ApiController]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [Route("/api/[controller]/[action]")]
    [SwaggerResponse(200, "Request is accepted, processed and response contains requested data.")]
    public class TempController(CachedData cachedData, Lazy<IDbProvider> lazyDbProvider) : ControllerBase
    {
        /// <summary>
        /// Returns list of Pots, owned by specified user, in any state.
        /// </summary>
        /// <param name="initData">Value of <see href="https://core.telegram.org/bots/webapps#initializing-mini-apps">initData</see> Telegram property.</param>
        /// <returns>List of <see cref="PotInfo"/>.</returns>
        [HttpGet]
        public ActionResult<List<PotInfo>> AllPotsByUser(
            [Required(AllowEmptyStrings = false), InitDataValidation, FromHeader(Name = BackendOptions.TelegramInitDataHeaderName)] string initData)
        {
            if (!ModelState.IsValid)
            {
                return new BadRequestObjectResult(new ValidationProblemDetails(ModelState));
            }

            var tgUser = InitDataValidationAttribute.GetUserDataWithoutValidation(initData);

            var db = lazyDbProvider.Value.MainDb;

            var pots = db.Table<Pot>().Where(x => x.OwnerUserId == tgUser.Id).OrderByDescending(x => x.Created).ToList();
            if (pots.Count == 0)
            {
                return (List<PotInfo>)[];
            }

            var creator = db.Get<User>(tgUser.Id);

            return pots.Select(x =>
                {
                    var jetton = cachedData.AllJettons[x.JettonMaster];
                    return PotInfo.Create(x, jetton, creator);
                })
                .ToList();
        }

        /// <summary>
        /// Returns all Pots in active state.
        /// </summary>
        /// <returns>Array of <see cref="PotInfo"/>.</returns>
        [HttpGet]
        public ActionResult<List<PotInfo>> AllActivePots()
        {
            return cachedData.ActivePots
                .Select(kv => kv.Value)
                .OrderByDescending(x => x.Created)
                .Select(x =>
                {
                    var jetton = cachedData.AllJettons[x.JettonMaster];
                    var creator = cachedData.ActivePotOwners[x.OwnerUserId];
                    return PotInfo.Create(x, jetton, creator);
                })
                .ToList();
        }
    }
}
