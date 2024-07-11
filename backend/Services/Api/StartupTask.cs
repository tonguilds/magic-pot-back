namespace MagicPot.Backend.Services.Api
{
    using MagicPot.Backend;
    using MagicPot.Backend.Data;
    using MagicPot.Backend.Utils;
    using Microsoft.Extensions.Options;
    using RecurrentTasks;

    public class StartupTask(ILogger<StartupTask> logger, IOptions<BackendOptions> options, IDbProvider dbProvider, TonApiService tonApiService)
        : IRunnable
    {
        public static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

        public async Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            var haveChanges = await PreloadJettons();

            // Force to run only once.
            currentTask.Options.Interval = TimeSpan.Zero;

            if (haveChanges)
            {
                scopeServiceProvider.ReloadCachedData();
            }
        }

        public async Task<bool> PreloadJettons()
        {
            var list = dbProvider.MainDb.Table<Jetton>().ToList();
            var count = 0;
            var changed = false;

            foreach (var (name, address) in options.Value.WellKnownJettons)
            {
                count++;
                var adr = AddressConverter.ToContract(address);
                var jetton = list.Find(x => x.Address == adr);
                if (jetton == null)
                {
                    jetton = await tonApiService.GetJettonInfo(address);
                    if (jetton == null)
                    {
                        logger.LogError("Failed to get info for Jetton '{Name}' {Address}", name, address);
                    }
                    else
                    {
                        dbProvider.MainDb.Insert(jetton);
                        changed = true;
                        logger.LogInformation("New Jetton saved: {Symbol} {Address} ({Name})", jetton.Symbol, jetton.Address, jetton.Name);
                    }
                }
            }

            logger.LogDebug("Checked all {Count} well-known jettons", count);

            return changed;
        }
    }
}
