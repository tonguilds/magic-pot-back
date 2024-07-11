namespace MagicPot.Backend.Services.Indexer
{
    using MagicPot.Backend.Data;
    using RecurrentTasks;

    public class SyncTask(IDbProvider dbProvider, BlockchainReader blockchainReader)
        : IRunnable
    {
        public static readonly TimeSpan DefaultInterval = TimeSpan.FromMinutes(5);

        public async Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            // Init TonClient and retry if needed, before actually syncing entities.
            currentTask.Options.Interval = GetRetryDelay(currentTask.RunStatus.FailsCount);

            var db = dbProvider.MainDb;

            var lastSeqno = db.Find<Settings>(Settings.KeyLastSeqno);
            var lastSeqnoValue = lastSeqno?.LongValue ?? 0;

            var seqno = await blockchainReader.EnsureSynced(lastSeqnoValue);

            // Write occasionally to not spam our DB
            if (seqno - lastSeqnoValue > 19)
            {
                lastSeqno ??= new Settings(Settings.KeyLastSeqno, 0L);
                lastSeqno.LongValue = seqno;
                dbProvider.MainDb.InsertOrReplace(lastSeqno);
            }
        }

        private static TimeSpan GetRetryDelay(int failsCount)
        {
            return failsCount switch
            {
                0 => TimeSpan.FromSeconds(5),
                1 => TimeSpan.FromSeconds(5),
                2 => TimeSpan.FromSeconds(5),
                3 => TimeSpan.FromSeconds(10),
                4 => TimeSpan.FromSeconds(15),
                5 => TimeSpan.FromSeconds(30),
                6 => TimeSpan.FromSeconds(60),
                7 => TimeSpan.FromMinutes(2),
                8 => TimeSpan.FromMinutes(5),
                9 => TimeSpan.FromMinutes(10),
                _ => TimeSpan.FromMinutes(30),
            };
        }
    }
}
