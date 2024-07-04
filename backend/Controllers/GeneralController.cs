namespace MagicPot.Backend.Controllers
{
    using System.Net.Mime;
    using MagicPot.Backend.Services;
    using Microsoft.AspNetCore.Mvc;
    using Swashbuckle.AspNetCore.Annotations;

    [ApiController]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [Route("/api/[controller]/[action]")]
    [SwaggerResponse(200, "Request is accepted, processed and response contains requested data.")]
    public class GeneralController(CachedData cachedData) : ControllerBase
    {
        /// <summary>
        /// Returns general backend data.
        /// </summary>
        [HttpGet]
        public ActionResult<BackendStatus> Status()
        {
            return new BackendStatus
            {
                InMainnet = cachedData.InMainnet,
                KnownJettonCount = cachedData.KnownJettons.Count,
            };
        }

        public class BackendStatus
        {
            public required bool InMainnet { get; set; }

            public required int KnownJettonCount { get; set; }
        }
    }
}
