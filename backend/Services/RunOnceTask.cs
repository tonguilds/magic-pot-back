namespace MagicPot.Backend.Services
{
    using MagicPot.Backend.Data;
    using Microsoft.Extensions.Options;
    using RecurrentTasks;

    public class RunOnceTask(ILogger<RunOnceTask> logger, IOptions<BackendOptions> options, IDbProvider dbProvider, TonApiService tonApiService)
        : IRunnable
    {
        public static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

        public async Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            await PreloadJettons();

            // Force to run only once.
            currentTask.Options.Interval = TimeSpan.Zero;
        }

        public async Task PreloadJettons()
        {
            var list = dbProvider.MainDb.Table<Jetton>().ToList();
            var count = 0;

            foreach (var (name, address) in options.Value.WellKnownJettons)
            {
                count++;
                var adr = TonLibDotNet.Utils.AddressUtils.Instance.SetBounceable(address, true);
                var existing = list.Find(x => x.Address == adr);
                if (existing == null)
                {
                    existing = await tonApiService.GetJettonInfo(address);
                    if (existing == null)
                    {
                        logger.LogError("Failed to get info for Jetton '{Name}' {Address}", name, address);
                    }
                    else
                    {
                        dbProvider.MainDb.Insert(existing);
                        logger.LogInformation("New Jetton saved: {Symbol} {Address} ({Name})", existing.Symbol, existing.Address, existing.Name);
                    }
                }
            }

            logger.LogDebug("Checked all {Count} well-known jettons", count);
        }
    }
}
