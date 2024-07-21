namespace MagicPot.Backend.Services.Indexer
{
    using System.Numerics;
    using MagicPot.Backend.Data;
    using RecurrentTasks;

    public class DetectUserJettonAddressesTask(ILogger<DetectUserJettonAddressesTask> logger, IDbProvider dbProvider, BlockchainReader blockchainReader, INotificationService notificationService)
        : IRunnable
    {
        public static readonly TimeSpan DefaultInterval = TimeSpan.FromSeconds(10);

        private const int MaxBatch = 100;

        public async Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            var changed = false;
            for (var i = 0; i < MaxBatch; i++)
            {
                var item = dbProvider.MainDb.Find<UserJettonWallet>(x => x.JettonWallet == null);
                if (item == null)
                {
                    break;
                }

                item.JettonWallet = await blockchainReader.GetJettonWallet(item.JettonMaster, item.MainWallet);

                var balance = await blockchainReader.GetJettonBalance(item.JettonWallet);
                if (balance == null)
                {
                    item.Balance = 0;
                }
                else
                {
                    var jetton = dbProvider.MainDb.Get<Jetton>(x => x.Address == item.JettonMaster);
                    item.Balance = (decimal)(balance.Value / (BigInteger)Math.Pow(10, jetton.Decimals));
                }

                item.Updated = DateTimeOffset.UtcNow;

                dbProvider.MainDb.Update(item);
                logger.LogDebug("Saved JettonWallet {Address} for user {Wallet} and jetton {Master}, balance {Balance}", item.JettonWallet, item.MainWallet, item.JettonMaster, item.Balance);
                changed = true;
            }

            if (changed)
            {
                notificationService.TryRun<Api.CachedData>();
            }
        }
    }
}
