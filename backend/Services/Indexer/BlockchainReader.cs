namespace MagicPot.Backend.Services.Indexer
{
    using System.Diagnostics.CodeAnalysis;
    using System.Numerics;
    using MagicPot.Backend.Data;
    using MagicPot.Backend.Utils;
    using TonLibDotNet;
    using TonLibDotNet.Cells;
    using TonLibDotNet.Types.Msg;

    public class BlockchainReader(ILogger<BlockchainReader> logger, ITonClient tonClient)
    {
        public async Task<long> EnsureSynced(long lastKnownSeqno = 0)
        {
            await tonClient.InitIfNeeded();
            var blockId = await tonClient.Sync();
            logger.LogDebug("Synced to masterchain block {Seqno}.", blockId.Seqno);

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

        public async Task<bool> CheckNewPotTransactions(Pot pot, Jetton jetton, IDbProvider dbProvider)
        {
            await tonClient.InitIfNeeded();

            var foundNew = false;
            var db = dbProvider.MainDb;

            var state = await tonClient.RawGetAccountState(pot.Address);

            if (state.LastTransactionId.Lt == pot.SyncLt)
            {
                pot.SyncUtime = state.SyncUtime;
                db.Update(pot);
                logger.LogTrace("Pot {Key} unchanged with lt={Lt}, sync {Time}", pot.Key, pot.SyncLt, pot.SyncUtime);
                return foundNew;
            }

            await foreach (var tx in EnumerateTransactions(pot.Address, state.LastTransactionId, pot.SyncLt))
            {
                var existing = db.Find<PotTransaction>(x => x.PotId == pot.Id && x.Hash == tx.TransactionId.Hash);
                if (existing != null)
                {
                    continue;
                }

                var ptx = new PotTransaction
                {
                    PotId = pot.Id,
                    State = PotTransactionState.Unknown,
                    Hash = tx.TransactionId.Hash,
                    Notified = tx.Utime,
                };

                if (tx.InMsg == null
                    || !TryParseJettonTransferNotification(tx.InMsg, out var jettonWalletAddress, out var queryId, out var userWalletAddres, out var amount))
                {
                    db.Insert(ptx);
                    logger.LogDebug("Pot {Key} tx {Hash} at {Time} is not a jetton transfer, ignored", pot.Key, ptx.Hash, ptx.Notified);
                    continue;
                }

                jettonWalletAddress = AddressConverter.ToContract(jettonWalletAddress);
                userWalletAddres = AddressConverter.ToUser(userWalletAddres);

                if (jettonWalletAddress != pot.JettonWallet)
                {
                    ptx.State = PotTransactionState.FakeTransfer;
                    db.Insert(ptx);
                    logger.LogDebug("Pot {Key} tx {Hash} at {Time} is a FAKE jetton transfer (from unknown {Address}), ignored", pot.Key, ptx.Hash, ptx.Notified, jettonWalletAddress);
                    continue;
                }

                ptx.State = PotTransactionState.Processing;
                ptx.Sender = userWalletAddres;
                ptx.Amount = (decimal)amount / (decimal)Math.Pow(10, 9);
                db.Insert(ptx);
                logger.LogInformation("Pot {Key} tx {Hash} at {Time} found new jetton transfer (user {Address})", pot.Key, ptx.Hash, ptx.Notified, ptx.Sender);
                foundNew = true;
            }

            pot.SyncLt = state.LastTransactionId.Lt;
            pot.SyncUtime = state.SyncUtime;
            db.Update(pot);
            logger.LogInformation("Pot {Key} updated to lt={Lt}, sync {Time}", pot.Key, pot.SyncLt, pot.SyncUtime);

            return foundNew;
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
