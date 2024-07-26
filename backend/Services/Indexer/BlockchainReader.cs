namespace MagicPot.Backend.Services.Indexer
{
    using System.Diagnostics.CodeAnalysis;
    using System.Numerics;
    using MagicPot.Backend.Utils;
    using TonLibDotNet;
    using TonLibDotNet.Cells;
    using TonLibDotNet.Types.Msg;
    using TonLibDotNet.Utils;

    public class BlockchainReader(ITonClient tonClient)
    {
        public async Task<long> EnsureSynced(long lastKnownSeqno = 0)
        {
            await tonClient.InitIfNeeded();
            var blockId = await tonClient.Sync();

            if (blockId.Seqno < lastKnownSeqno)
            {
                tonClient.Deinit();
                throw new SyncException(blockId.Seqno, lastKnownSeqno);
            }

            return blockId.Seqno;
        }

        public async Task<string> GetJettonWallet(string jettonMaster, string mainWallet)
        {
            await tonClient.InitIfNeeded();

            var jw = await TonLibDotNet.Recipes.Tep74Jettons.Instance.GetWalletAddress(tonClient, jettonMaster, mainWallet);
            return AddressConverter.ToContract(jw);
        }

        public async Task<BigInteger?> GetJettonBalance(string jettonWallet)
        {
            await tonClient.InitIfNeeded();

            try
            {
                var (balance, _, _, _) = await TonLibDotNet.Recipes.Tep74Jettons.Instance.GetWalletData(tonClient, jettonWallet);
                return balance;
            }
            catch (TonLibNonZeroExitCodeException)
            {
                return null;
            }
        }

        public async Task<(DateTimeOffset SyncTime, TonLibDotNet.Types.Internal.TransactionId LastTransaction)> GetAccountState(string account)
        {
            await tonClient.InitIfNeeded();
            var state = await tonClient.RawGetAccountState(account);
            return (state.SyncUtime, state.LastTransactionId);
        }

        public async IAsyncEnumerable<TonLibDotNet.Types.Raw.Transaction> EnumerateTransactions(string address, TonLibDotNet.Types.Internal.TransactionId start, long endLt)
        {
            await tonClient.InitIfNeeded();

            while (!start.IsEmpty())
            {
                var res = await tonClient.RawGetTransactions(address, start);
                if (res.TransactionsList.Count == 0)
                {
                    yield break;
                }

                foreach (var tx in res.TransactionsList)
                {
                    if (tx.TransactionId.Lt == endLt)
                    {
                        yield break;
                    }

                    yield return tx;
                }

                if (res.PreviousTransactionId.Lt == endLt)
                {
                    yield break;
                }
                else
                {
                    start = res.PreviousTransactionId;
                }
            }
        }

        public bool TryParseJettonTransferNotification(
            TonLibDotNet.Types.Raw.Message msg,
            [NotNullWhen(true)] out string? jettonWalletAddress,
            [NotNullWhen(true)] out long? queryId,
            [NotNullWhen(true)] out string? userWalletAddress,
            [NotNullWhen(true)] out BigInteger? amount)
        {
            amount = default;
            userWalletAddress = default;
            queryId = default;
            jettonWalletAddress = msg.Source.Value;

            if (msg.MsgData is not DataRaw data || string.IsNullOrWhiteSpace(data.Body))
            {
                return false;
            }

            var slice = Boc.ParseFromBase64(data.Body).RootCells[0].BeginRead();

            if (!slice.TryCanLoad(32))
            {
                return false;
            }

            var op = slice.LoadInt(32);
            if (op != 0x7362d09c)
            {
                return false;
            }

            queryId = slice.LoadLong(64);
            amount = slice.LoadCoinsToBigInt();
            userWalletAddress = slice.LoadAddressIntStd();

            return true;
        }

        public class SyncException(long syncSeqno, long lastKnownSeqno)
            : Exception($"Sync failed: seqno {syncSeqno} is less than last known {lastKnownSeqno}.")
        {
            // Nothing
        }
    }
}
