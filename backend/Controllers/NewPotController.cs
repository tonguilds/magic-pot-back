namespace MagicPot.Backend.Controllers
{
    using System.ComponentModel.DataAnnotations;
    using System.Net.Mime;
    using MagicPot.Backend.Attributes;
    using MagicPot.Backend.Data;
    using MagicPot.Backend.Services;
    using Microsoft.AspNetCore.Mvc;
    using Swashbuckle.AspNetCore.Annotations;

    [ApiController]
    [Produces(MediaTypeNames.Application.Json)]
    [Route("/api/[controller]/[action]")]
    [SwaggerResponse(200, "Request is accepted, processed and response contains requested data.")]
    public class NewPotController(CachedData cachedData, Lazy<IDbProvider> lazyDbProvider, ILogger<NewPotController> logger) : ControllerBase
    {
        [HttpPost]
        [Consumes(MediaTypeNames.Application.Json)]
        public async Task<CheckResult> CheckNewPotAsJson(
            [Required, InitDataValidation, FromHeader(Name = BackendOptions.TelegramInitDataHeaderName)] string initData,
            [Required] NewPotModel model)
        {
            await ValidateJetton(model);

            return ModelState.IsValid ? new CheckResult(true, null) : new CheckResult(false, new ValidationProblemDetails(ModelState).Errors);
        }

        [HttpPost]
        [Consumes(MediaTypeNames.Multipart.FormData)]
        public async Task<CheckResult> CheckNewPotAsForm(
            [Required, InitDataValidation, FromHeader(Name = BackendOptions.TelegramInitDataHeaderName)] string initData,
            [Required][FromForm] NewPotModel model)
        {
            await ValidateJetton(model);

            return ModelState.IsValid ? new CheckResult(true, null) : new CheckResult(false, new ValidationProblemDetails(ModelState).Errors);
        }

        protected async Task<Jetton?> ValidateJetton(NewPotModel model)
        {
            if (!string.IsNullOrEmpty(model.TokenName))
            {
                if (!cachedData.Options.WellKnownJettons.TryGetValue(model.TokenName, out var adr))
                {
                    ModelState.AddModelError(nameof(model.TokenName), Messages.UnknownTokenName);
                    return null;
                }

                model.TokenAddress = adr;
            }

            if (!string.IsNullOrEmpty(model.TokenAddress))
            {
                if (!TonLibDotNet.Utils.AddressUtils.Instance.TrySetBounceable(model.TokenAddress, true, out var adr))
                {
                    ModelState.AddModelError(nameof(model.TokenAddress), Messages.InvalidTokenAddress);
                    return null;
                }

                var db = lazyDbProvider.Value.MainDb;

                var existing = db.Find<Jetton>(x => x.Address == adr);
                if (existing != null)
                {
                    return existing;
                }

                var tonapi = HttpContext.RequestServices.GetRequiredService<TonApiService>();
                var jetton = await tonapi.GetJettonInfo(adr);
                if (jetton == null)
                {
                    ModelState.AddModelError(nameof(model.TokenAddress), Messages.AddressIsNotAJetton);
                    return null;
                }

                db.BeginTransaction();

                existing = db.Find<Jetton>(x => x.Address == jetton.Address);
                if (existing == null)
                {
                    db.Insert(jetton);
                    logger.LogInformation("New Jetton saved: {Symbox} {Address} ({Name})", jetton.Symbol, jetton.Address, jetton.Name);
                }

                db.Commit();

                HttpContext.ReloadCachedData();

                return db.Find<Jetton>(x => x.Address == jetton.Address);
            }

            return null;
        }

        public record CheckResult(bool Valid, IDictionary<string, string[]>? Errors);
    }
}
