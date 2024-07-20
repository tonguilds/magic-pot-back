namespace MagicPot.Backend.Services.Indexer
{
    using MagicPot.Backend.Data;
    using RecurrentTasks;

    public class PotUpdateTask(ILogger<PotUpdateTask> logger, IDbProvider dbProvider, BlockchainReader blockchainReader, INotificationService notificationService) : IRunnable
    {
        public static readonly TimeSpan DefaultInterval = TimeSpan.FromSeconds(5);

        private const int MaxBatch = 100;

        public async Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            currentTask.Options.Interval = DefaultInterval;

            var db = dbProvider.MainDb;

            for (var i = 0; i < MaxBatch; i++)
            {
                var pot = db.Table<Pot>().OrderBy(x => x.NextUpdate).FirstOrDefault();
                if (pot == null)
                {
                    logger.LogTrace("Queue is empty");
                    return;
                }

                var wait = pot.NextUpdate - DateTimeOffset.UtcNow;
                if (wait > TimeSpan.Zero)
                {
                    if (wait < DefaultInterval)
                    {
                        currentTask.Options.Interval = wait;
                    }

                    logger.LogTrace("Next at {At}, will wait", pot.NextUpdate);
                    return;
                }

                var changed = await Update(pot).ConfigureAwait(false);

                pot.UpdateNextUpdate();
                db.Update(pot);

                if (changed)
                {
                    notificationService.TryRun<Api.CachedData>();
                }
            }
        }

        protected async Task<bool> Update(Pot pot)
        {
            var changed = false;
            var db = dbProvider.MainDb;

            if (string.IsNullOrWhiteSpace(pot.JettonWallet))
            {
                pot.JettonWallet = await blockchainReader.GetJettonWallet(pot.JettonMaster, pot.Address);
                db.Update(pot);
                logger.LogInformation("Got JettonWallet for pot {Key}: {Address}", pot.Key, pot.JettonWallet);
                changed = true;
            }

            var jetton = dbProvider.MainDb.Get<Jetton>(x => x.Address == pot.JettonMaster);

            var haveNew = await blockchainReader.CheckNewPotTransactions(pot, jetton, dbProvider).ConfigureAwait(false);
            if (!haveNew)
            {
                return changed;
            }

            var list = db.Table<PotTransaction>()
                .Where(x => x.PotId == pot.Id && x.State == PotTransactionState.Processing)
                .OrderBy(x => x.Notified)
                .ToList();

            foreach (var tx in list)
            {
                if (pot.Charged == null)
                {
                    if (tx.Sender != pot.OwnerUserAddress)
                    {
                        tx.State = PotTransactionState.BeforeCharge;
                        db.Update(tx);
                    }
                    else if (tx.Amount < pot.InitialSize)
                    {
                        tx.State = PotTransactionState.TooSmallForCharge;
                        db.Update(tx);
                    }
                    else
                    {
                        tx.State = PotTransactionState.Charge;
                        db.Update(tx);

                        pot.Charged = tx.Notified;
                    }
                }

                pot.TotalSize += tx.Amount;
                db.Update(pot);
                changed = true;
            }

            return changed;
        }
    }
}
