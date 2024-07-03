namespace MagicPot.Backend
{
    using MagicPot.Backend.Services;
    using Microsoft.Extensions.Configuration;
    using RecurrentTasks;
    using TonLibDotNet;
    using TonLibDotNet.Types;

    public static class StartupIndexer
    {
        public static IReadOnlyList<Type> RegisteredTasks { get; private set; } = [];

        public static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            var optionsSection = context.Configuration.GetSection("BackendOptions");
            services.Configure<BackendOptions>(optionsSection);

            var bo = new BackendOptions();
            optionsSection.Bind(bo);

            services.AddScoped<IDbProvider, DbProvider>();
            services.AddScoped(sp => new Lazy<IDbProvider>(() => sp.GetRequiredService<IDbProvider>()));

            var dir = Path.GetDirectoryName(bo.CacheDirectory)!;
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            services.Configure<TonOptions>(context.Configuration.GetSection("TonOptions"));
            services.Configure<TonOptions>(o => o.Options.KeystoreType = new KeyStoreTypeDirectory(dir));
            services.AddSingleton<ITonClient, TonClient>();

            services.AddHttpClient<TonApiService>();

            services.AddTask<RunOnceTask>(o => o.AutoStart(RunOnceTask.Interval, TimeSpan.FromSeconds(3)));

            RegisteredTasks =
                [
                    typeof(ITask<RunOnceTask>),
                ];
        }
    }
}
