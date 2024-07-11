namespace MagicPot.Backend
{
    using MagicPot.Backend.Data;
    using MagicPot.Backend.Services.Indexer;
    using Microsoft.Extensions.Configuration;
    using RecurrentTasks;
    using TonLibDotNet;
    using TonLibDotNet.Types;

    public static class StartupIndexer
    {
        public static IReadOnlyList<Type> RegisteredTasks { get; private set; } = [];

        public static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            services.AddHttpClient();

            var optionsSection = context.Configuration.GetSection("BackendOptions");
            services.Configure<BackendOptions>(optionsSection);

            var bo = new BackendOptions();
            optionsSection.Bind(bo);

            services.AddScoped<IDbProvider, DbProvider>();
            services.AddScoped(sp => new Lazy<IDbProvider>(() => sp.GetRequiredService<IDbProvider>()));

            var dir = Path.GetFullPath(bo.CacheDirectory)!;
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            services.Configure<TonOptions>(context.Configuration.GetSection("TonOptions"));
            services.Configure<TonOptions>(o => o.Options.KeystoreType = new KeyStoreTypeDirectory(dir));
            services.AddSingleton<ITonClient, TonClient>();

            services.AddScoped<BlockchainReader>();

            services.AddTask<HealthReportTask>(o => o.AutoStart(HealthReportTask.DefaultInterval, TimeSpan.FromSeconds(3)));
            services.AddTask<SyncTask>(o => o.AutoStart(SyncTask.DefaultInterval));
            services.AddTask<PrecacheMnemonicsTask>(o => o.AutoStart(PrecacheMnemonicsTask.DefaultInterval));
            services.AddTask<DetectUserJettonAddressesTask>(o => o.AutoStart(DetectUserJettonAddressesTask.DefaultInterval));
            services.AddTask<PotUpdateTask>(o => o.AutoStart(PotUpdateTask.DefaultInterval));

            RegisteredTasks =
                [
                    typeof(ITask<HealthReportTask>),
                    typeof(ITask<SyncTask>),
                    typeof(ITask<PrecacheMnemonicsTask>),
                    typeof(ITask<DetectUserJettonAddressesTask>),
                    typeof(ITask<PotUpdateTask>),
                ];
        }
    }
}
