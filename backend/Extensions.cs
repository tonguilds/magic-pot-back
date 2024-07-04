namespace MagicPot.Backend
{
    using MagicPot.Backend.Services;
    using RecurrentTasks;

    public static class Extensions
    {
        public static void ReloadCachedData(this HttpContext httpContext)
        {
            ReloadCachedData(httpContext.RequestServices);
        }

        public static void ReloadCachedData(this IServiceProvider serviceProvider)
        {
            serviceProvider.GetRequiredService<ITask<CachedData>>().TryRunImmediately();
        }
    }
}
