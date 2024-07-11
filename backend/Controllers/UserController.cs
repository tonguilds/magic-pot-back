namespace MagicPot.Backend.Controllers
{
    using System.ComponentModel.DataAnnotations;
    using System.Net.Mime;
    using MagicPot.Backend.Attributes;
    using MagicPot.Backend.Data;
    using Microsoft.AspNetCore.Mvc;
    using Swashbuckle.AspNetCore.Annotations;

    [ApiController]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [Route("/api/[controller]/[action]")]
    [SwaggerResponse(200, "Request is accepted, processed and response contains requested data.")]
    public class UserController(IDbProvider dbProvider, ILogger<UserController> logger) : ControllerBase
    {
        /// <summary>
        /// Returns some information about current user.
        /// </summary>
        /// <param name="initData">Telegram InitData string.</param>
        /// <returns><see cref="UserInfo"/> data.</returns>
        /// <remarks>
        /// A side-effect: username gets updated if needed.
        /// </remarks>
        [HttpGet]
        public ActionResult<UserInfo> GetInfo(
            [Required, InitDataValidation, FromHeader(Name = BackendOptions.TelegramInitDataHeaderName)] string initData)
        {
            if (!ModelState.IsValid)
            {
                return new BadRequestObjectResult(new ValidationProblemDetails(ModelState));
            }

            var tgUser = InitDataValidationAttribute.GetUserDataWithoutValidation(initData);

            var user = dbProvider.GetOrCreateUser(tgUser.Id, tgUser.Username);

            return new UserInfo
            {
                Id = user.Id,
                RefCode = Base36.Encode(user.Id),
                Points = user.Points,
            };
        }

        /// <summary>
        /// Creates new user and sets referrer to specified one.
        /// </summary>
        /// <param name="initData">Telegram InitData string.</param>
        /// <param name="refCode">Alphanumeric ref code of other user (referrer).</param>
        /// <returns><see cref="CreateUserResult"/> data.</returns>
        /// <remarks>
        /// Does nothing when user already exist, so it's OK to call it on every <b>/start</b> command.
        /// </remarks>
        [HttpPost]
        public ActionResult<CreateUserResult> CreateNew(
            [Required, InitDataValidation, FromHeader(Name = BackendOptions.TelegramInitDataHeaderName)] string initData,
            [MaxLength(20)] string? refCode)
        {
            if (!ModelState.IsValid)
            {
                return new BadRequestObjectResult(new ValidationProblemDetails(ModelState));
            }

            var tgUser = InitDataValidationAttribute.GetUserDataWithoutValidation(initData);
            var user = dbProvider.MainDb.Find<User>(tgUser.Id);
            if (user != null)
            {
                return new CreateUserResult(false);
            }

            user = new User()
            {
                Id = tgUser.Id,
                Username = tgUser.Username,
                Referrer = Base36.TryDecodeLong(refCode),
                Created = DateTimeOffset.UtcNow,
                Points = 0,
            };

            logger.LogInformation("New User {Id} added (referrer {RefId})", tgUser, user.Referrer);

            dbProvider.MainDb.Insert(user);

            return new CreateUserResult(true);
        }

        public class UserInfo
        {
            public required long Id { get; set; }

            /// <summary>
            /// Own ref.code (to give to other users).
            /// </summary>
            public required string RefCode { get; set; }

            public required int Points { get; set; }
        }

        public class CreateUserResult(bool created)
        {
            /// <summary>
            /// <b>true</b> when new user had been created, and <b>false</b> when user has already been created.
            /// </summary>
            public bool Created { get; set; } = created;
        }
    }
}
