namespace MagicPot.Backend
{
    using System.Text.Json;
    using Microsoft.Extensions.Options;

    public class IndexerHealthUpdateMiddleware(IOptions<BackendOptions> options) : IMiddleware
    {
        private readonly PathString path = options?.Value.HealthReportPath ?? default;

        public static List<HealthMiddleware.TaskInfo> Data { get; private set; } = [];

        public Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var req = context.Request;

            return req.Method == "POST" && path.HasValue && path.Equals(context.Request.Path)
                ? LoadData(req)
                : next(context);
        }

        protected async Task LoadData(HttpRequest request)
        {
            Data = await JsonSerializer.DeserializeAsync<List<HealthMiddleware.TaskInfo>>(request.Body, JsonSerializerOptions.Default) ?? [];
        }
    }
}
