namespace MagicPot.Backend.Services.Indexer
{
    using MagicPot.Backend;
    using MagicPot.Backend.Data;
    using MagicPot.Backend.Utils;
    using RecurrentTasks;
    using TonLibDotNet;
    using TonLibDotNet.Types.Wallet;

    public class PrecacheMnemonicsTask(ILogger<PrecacheMnemonicsTask> logger, IDbProvider dbProvider, ITonClient tonClient) : IRunnable
    {
        public static readonly TimeSpan DefaultInterval = TimeSpan.FromMinutes(5);

        private const int MinPoolSize = 50;
        private const int MaxPoolSize = 100;

        public async Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            var count = dbProvider.MainDb.Table<PrecachedMnemonic>().Count();
            if (count > MinPoolSize)
            {
                logger.LogDebug("Current pool size: {Count}, nothing to do.", count);
                return;
            }

            await tonClient.InitIfNeeded();

            // Surprise! Even for testnet, wallet.ton.org uses mainnet value :(
            var walletId = 698983191; // tonClient.OptionsInfo!.ConfigInfo.DefaultWalletId

            count = MaxPoolSize - count;
            for (var i = 0; i < count; i++)
            {
                var key = await tonClient.CreateNewKey();
                var words = await tonClient.ExportKey(key);
                var ias = new V3InitialAccountState() { PublicKey = key.PublicKey, WalletId = walletId };
                var address = await tonClient.GetAccountAddress(ias, 0, 0);
                await tonClient.DeleteKey(key);

                var item = new PrecachedMnemonic
                {
                    Address = AddressConverter.ToContract(address.Value),
                    Mnemonic = string.Join(' ', words.WordList),
                };
                dbProvider.MainDb.Insert(item);
            }

            logger.LogInformation("Added {Count} new items to pool.", count);
        }
    }
}
