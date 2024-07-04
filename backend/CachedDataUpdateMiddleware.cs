namespace MagicPot.Backend
{
    using MagicPot.Backend.Services;
    using Microsoft.Extensions.Options;
    using RecurrentTasks;

    public class CachedDataUpdateMiddleware(IOptions<BackendOptions> options) : IMiddleware
    {
        private readonly PathString path = options?.Value.SearchCacheUpdatePath ?? default;

        public Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (!path.HasValue || !path.Equals(context.Request.Path))
            {
                return next(context);
            }

            var task = context.RequestServices.GetRequiredService<ITask<CachedData>>();
            task.TryRunImmediately();

            return Task.CompletedTask;
        }
    }
}
