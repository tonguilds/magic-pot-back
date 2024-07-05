namespace MagicPot.Backend.Services
{
    using MagicPot.Backend.Data;
    using RecurrentTasks;
    using TonLibDotNet;

    public class DetectUserJettonAddressesTask(ILogger<DetectUserJettonAddressesTask> logger, IDbProvider dbProvider, ITonClient tonClient)
        : IRunnable
    {
        public static readonly TimeSpan DefaultInterval = TimeSpan.FromSeconds(10);

        private const int MaxBatch = 100;

        public async Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            await tonClient.InitIfNeeded();

            for (var i = 0; i < MaxBatch; i++)
            {
                var item = dbProvider.MainDb.Find<UserJettonWallet>(x => x.JettonWallet == null);
                if (item == null)
                {
                    break;
                }

                item.JettonWallet = await TonLibDotNet.Recipes.Tep74Jettons.Instance.GetWalletAddress(tonClient, item.JettonMaster, item.MainWallet);
                item.JettonWallet = TonLibDotNet.Utils.AddressUtils.Instance.SetBounceable(item.JettonWallet, true);
                dbProvider.MainDb.Update(item);
                logger.LogDebug("Saved JettonWallet {Address} for user {Wallet} and jetton {Master}", item.JettonWallet, item.MainWallet, item.JettonMaster);
            }
        }
    }
}
