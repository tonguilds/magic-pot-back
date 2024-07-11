namespace MagicPot.Backend.Services.Indexer
{
    using MagicPot.Backend.Data;
    using RecurrentTasks;

    public class DetectUserJettonAddressesTask(ILogger<DetectUserJettonAddressesTask> logger, IDbProvider dbProvider, BlockchainReader blockchainReader)
        : IRunnable
    {
        public static readonly TimeSpan DefaultInterval = TimeSpan.FromSeconds(10);

        private const int MaxBatch = 100;

        public async Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            for (var i = 0; i < MaxBatch; i++)
            {
                var item = dbProvider.MainDb.Find<UserJettonWallet>(x => x.JettonWallet == null);
                if (item == null)
                {
                    break;
                }

                item.JettonWallet = await blockchainReader.GetJettonWallet(item.JettonMaster, item.MainWallet);
                dbProvider.MainDb.Update(item);
                logger.LogDebug("Saved JettonWallet {Address} for user {Wallet} and jetton {Master}", item.JettonWallet, item.MainWallet, item.JettonMaster);
            }
        }
    }
}
