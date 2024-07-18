namespace MagicPot.Backend.Controllers
{
    using System.Net.Mime;
    using MagicPot.Backend.Services.Api;
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
                InMainnet = Program.InMainnet,
                KnownJettonCount = cachedData.AllJettons.Count,
                MasterchainSeqno = cachedData.LastKnownSeqno,
            };
        }

        public class BackendStatus
        {
            public required bool InMainnet { get; set; }

            public required int KnownJettonCount { get; set; }

            public required long MasterchainSeqno { get; set; }
        }
    }
}
