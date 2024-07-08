namespace MagicPot.Bot
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;

    public class RobotsTxtMiddleware : IMiddleware
    {
        private const string Contents = "User-agent: * \r\nDisallow: / \r\n";

        public Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var request = context.Request;

            if (request.Path != "/robots.txt")
            {
                return next.Invoke(context);
            }

            var response = context.Response;

            response.StatusCode = StatusCodes.Status200OK;
            response.ContentType = "text/plain";
            return response.WriteAsync(Contents);
        }
    }
}
