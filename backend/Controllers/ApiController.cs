namespace MagicPot.Backend.Controllers
{
    using System.ComponentModel.DataAnnotations;
    using MagicPot.Backend.Attributes;
    using MagicPot.Backend.Data;
    using MagicPot.Backend.Services;
    using Microsoft.AspNetCore.Mvc;
    using Swashbuckle.AspNetCore.Annotations;

    [ApiController]
    [Consumes(System.Net.Mime.MediaTypeNames.Application.Json)]
    [Produces(System.Net.Mime.MediaTypeNames.Application.Json)]
    [Route("/api/[action]")]
    [SwaggerResponse(200, "Request is accepted, processed and response contains requested data.")]
    public class ApiController(CachedData cachedData, Lazy<IDbProvider> lazyDbProvider) : ControllerBase
    {
        public const string TelegramInitDataHeaderName = "X_InitData";

        /// <summary>
        /// Returns general configuration data.
        /// </summary>
        [HttpGet]
        public ActionResult<BackendConfig> Config()
        {
            return new BackendConfig
            {
                InMainnet = cachedData.InMainnet,
            };
        }

        /// <summary>
        /// Returns all pools, ownder (created) by current user.
        /// </summary>
        /// <param name="initData">InitData string from Telegram.</param>
        /// <param name="includeClosed">Should (or not) include closed pools too.</param>
        /// <returns>List of pools.</returns>
        [HttpGet]
        public ActionResult<List<Pool>> GetMyPools(
            [Required, InitDataValidation, FromHeader(Name = TelegramInitDataHeaderName)] string initData,
            bool includeClosed = false)
        {
            var userId = InitDataValidationAttribute.GetUserIdWithoutValidation(initData);

            return cachedData.AllPools.Where(x => x.OwnerId == userId).Where(x => includeClosed || x.State != PoolState.Closed).ToList();
        }

        public class BackendConfig
        {
            public required bool InMainnet { get; set; }
        }
    }
}
