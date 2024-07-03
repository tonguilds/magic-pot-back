namespace MagicPot.Backend
{
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using TonLibDotNet;

    public static class Program
    {
        public const string StartAsIndexerArg = "--indexer";

        public static bool InIndexerMode { get; private set; }

        public static async Task Main(string[] args)
        {
            InIndexerMode = args.Contains(StartAsIndexerArg, StringComparer.OrdinalIgnoreCase);

            var host = (InIndexerMode ? CreateIndexerHostBuilder(args) : CreateApiHostBuilder(args)).Build();

            using (var scope = host.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<IDbProvider>();
                db.Migrate();

                var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(Program));
                VerifyNetType(db, logger, scope.ServiceProvider);
            }

            await host.RunAsync();
        }

        public static IHostBuilder CreateApiHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging(o => o.AddSystemdConsole())
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<StartupApi>();
                });

        public static IHostBuilder CreateIndexerHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging(o => o.AddSystemdConsole())
                .UseConsoleLifetime()
                .ConfigureServices(StartupIndexer.ConfigureServices);

        private static void VerifyNetType(IDbProvider db, ILogger logger, IServiceProvider serviceProvider)
        {
            var tonOpt = serviceProvider.GetRequiredService<IOptions<TonOptions>>();

            var inMainnet = tonOpt.Value.UseMainnet;

            var setting = db.MainDb.Find<Settings>(Settings.KeyNetType);
            if (setting == null)
            {
                setting = new Settings(Settings.KeyNetType, inMainnet);
                db.MainDb.Insert(setting);
            }
            else if (setting.BoolValue != inMainnet)
            {
                logger.LogError("Net type mismatch: saved mainnet='{Saved}', configured mainnet='{Configured}'. Erase db to start with new net type!", setting.BoolValue, inMainnet);
                throw new InvalidOperationException("Net type changed");
            }

            logger.Log(inMainnet ? LogLevel.Information : LogLevel.Warning, "Net type is mainnet: {Value}", inMainnet);
        }
    }
}
