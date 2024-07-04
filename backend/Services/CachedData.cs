namespace MagicPot.Backend.Services
{
    using MagicPot.Backend.Data;
    using Microsoft.Extensions.Options;
    using RecurrentTasks;

    public class CachedData : IRunnable
    {
        public bool InMainnet { get; private set; }

        public BackendOptions Options { get; private set; } = new();

        public long LastKnownSeqno { get; private set; }

        public List<Jetton> KnownJettons { get; private set; } = [];

        public List<Pool> AllPools { get; private set; } = [];

        public Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            Options = scopeServiceProvider.GetRequiredService<IOptionsSnapshot<BackendOptions>>().Value;

            var db = scopeServiceProvider.GetRequiredService<IDbProvider>();

            InMainnet = db.MainDb.Find<Settings>(Settings.KeyNetType)?.BoolValue ?? default;
            LastKnownSeqno = db.MainDb.Find<Settings>(Settings.KeyLastSeqno)?.LongValue ?? default;
            AllPools = db.MainDb.Table<Pool>().ToList();
            KnownJettons = db.MainDb.Table<Jetton>().ToList();

            var logger = scopeServiceProvider.GetRequiredService<ILogger<CachedData>>();
            logger.LogDebug(
                "Reloaded: InMainnet={Mainnet}, KnownJettons={Count}",
                InMainnet,
                KnownJettons.Count);

            return Task.CompletedTask;
        }
    }
}
