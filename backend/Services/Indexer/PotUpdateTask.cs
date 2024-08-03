namespace MagicPot.Backend.Services.Indexer
{
    using System.Runtime.CompilerServices;
    using MagicPot.Backend.Data;
    using MagicPot.Backend.Utils;
    using Microsoft.AspNetCore.Http.HttpResults;
    using RecurrentTasks;
    using TonLibDotNet.Types.Internal;

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
                    notificationService.TryRun<Api.ScheduledMessageSender>();
                }
            }
        }

        public async Task<bool> Update(Pot pot)
        {
            var changed = await UpdatePotJettonAddress(pot);

            var db = dbProvider.MainDb;

            var (syncTime, lastTx) = await blockchainReader.GetAccountState(pot.Address);
            if (lastTx.Lt == pot.SyncLt)
            {
                pot.SyncUtime = syncTime;
                dbProvider.MainDb.Update(pot);
                logger.LogTrace("Pot {Key} unchanged with lt={Lt}, sync {Time}", pot.Key, pot.SyncLt, pot.SyncUtime);
            }
            else
            {
                var jetton = db.Get<Jetton>(x => x.Address == pot.JettonMaster);
                changed |= await LoadNewTransactions(pot, lastTx, syncTime, jetton);
                changed |= ProcessNewTransactions(pot);
            }

            changed |= CheckForStolen(pot);

            return changed;
        }

        public async Task<bool> UpdatePotJettonAddress(Pot pot)
        {
            if (!string.IsNullOrWhiteSpace(pot.JettonWallet))
            {
                return false;
            }

            pot.JettonWallet = await blockchainReader.GetJettonWallet(pot.JettonMaster, pot.Address);
            dbProvider.MainDb.Update(pot);
            logger.LogInformation("Got JettonWallet for pot {Key}: {Address}", pot.Key, pot.JettonWallet);
            return true;
        }

        public async Task<bool> LoadNewTransactions(Pot pot, TransactionId lastTransaction, DateTimeOffset syncTime, Jetton jetton)
        {
            var found = false;
            var db = dbProvider.MainDb;

            await foreach (var tx in blockchainReader.EnumerateTransactions(pot.Address, lastTransaction, pot.SyncLt))
            {
                var existing = db.Find<Transaction>(x => x.PotId == pot.Id && x.Hash == tx.TransactionId.Hash);
                if (existing != null)
                {
                    continue;
                }

                var ptx = new Transaction
                {
                    PotId = pot.Id,
                    Hash = tx.TransactionId.Hash,
                    Time = tx.Utime,
                    Sender = string.Empty,
                    Amount = 0,
                    IsJettonTransfer = false,
                    OpCode = TransactionOpcode.DefaultNone,
                    State = TransactionState.Unprocessed,
                    UserId = null,
                    Referrer = null,
                };

                if (tx.InMsg == null)
                {
                    ptx.State = TransactionState.InvalidNoInMsg;
                    db.Insert(ptx);
                    logger.LogWarning("Ignored tx {Hash} at {Time} for pot {Key}: {State}", ptx.Hash, ptx.Time, pot.Key, ptx.State);
                    continue;
                }

                ptx.Amount = TonLibDotNet.Utils.CoinUtils.Instance.FromNano(tx.InMsg.Value);

                if (string.IsNullOrWhiteSpace(tx.InMsg.Source.Value))
                {
                    ptx.State = TransactionState.InvalidNoSender;
                    db.Insert(ptx);
                    logger.LogWarning("Ignored tx {Hash} at {Time} for pot {Key}: {State}", ptx.Hash, ptx.Time, pot.Key, ptx.State);
                    continue;
                }

                ptx.Sender = AddressConverter.ToUser(tx.InMsg.Source.Value);

                if (!blockchainReader.TryParseJettonTransferNotification(tx.InMsg, out var jettonWalletAddress, out var queryId, out var userWalletAddres, out var amount, out var forwardPayload))
                {
                    // Keep state Unprocessed, will process later.
                    ptx.State = TransactionState.Unprocessed;
                    db.Insert(ptx);
                    logger.LogInformation("Found non-jetton tx {Hash} at {Time} from {Sender} for pot {Key}: {State}", ptx.Hash, ptx.Time, ptx.Sender, pot.Key, ptx.State);
                    continue;
                }

                jettonWalletAddress = AddressConverter.ToContract(jettonWalletAddress);
                if (jettonWalletAddress != pot.JettonWallet)
                {
                    ptx.State = TransactionState.InvalidUnknownJetton;
                    db.Insert(ptx);
                    logger.LogWarning("Ignored unknown-jetton tx {Hash} at {Time} from {Sender} for pot {Key}: {State}", ptx.Hash, ptx.Time, ptx.Sender, pot.Key, ptx.State);
                    continue;
                }

                ptx.IsJettonTransfer = true;
                ptx.Sender = AddressConverter.ToUser(userWalletAddres);
                ptx.Amount = (decimal)amount / (decimal)Math.Pow(10, jetton.Decimals);

                if (forwardPayload != null)
                {
                    if (!PayloadEncoder.TryDecode(forwardPayload, out var encodedOpCode, out var encodedUserId, out var encodedReferrerAddress))
                    {
                        ptx.State = TransactionState.InvalidBadPayload;
                        db.Insert(ptx);
                        logger.LogWarning("Ignored 'corrupted' jetton tx {Hash} at {Time} from {Sender} for pot {Key}: {State}", ptx.Hash, ptx.Time, ptx.Sender, pot.Key, ptx.State);
                        continue;
                    }

                    ptx.OpCode = encodedOpCode;
                    ptx.UserId = encodedUserId;
                    ptx.Referrer = string.IsNullOrEmpty(encodedReferrerAddress) ? null : AddressConverter.ToUser(encodedReferrerAddress);

                    // Feat: self-ref or accidental(?) pot-ref are not allowed.
                    if (ptx.Referrer == userWalletAddres || ptx.Referrer == AddressConverter.ToUser(pot.Address))
                    {
                        ptx.Referrer = pot.OwnerUserAddress;
                    }
                }

                db.Insert(ptx);
                logger.LogInformation("Found jetton tx {Hash} at {Time} from {Sender} for pot {Key}: {Amount} coins", ptx.Hash, ptx.Time, ptx.Sender, pot.Key, ptx.Amount);
                found = true;
            }

            pot.SyncLt = lastTransaction.Lt;
            pot.SyncUtime = syncTime;
            pot.Touch();
            db.Update(pot);
            logger.LogInformation("Pot {Key} updated to lt={Lt}, sync {Time}", pot.Key, pot.SyncLt, pot.SyncUtime);

            return found;
        }

        public bool ProcessNewTransactions(Pot pot)
        {
            var db = dbProvider.MainDb;

            var list = db.Table<Transaction>()
                .Where(x => x.PotId == pot.Id && x.State == TransactionState.Unprocessed)
                .OrderBy(x => x.Time)
                .ToList();

            if (list.Count == 0)
            {
                return false;
            }

            foreach (var tx in list)
            {
                if (!tx.IsJettonTransfer)
                {
                    tx.State = TransactionState.UnknownIgnored;
                    db.Update(tx);
                    continue;
                }

                switch (tx.OpCode)
                {
                    case TransactionOpcode.PrizeTransfer:
                        var charged = false;
                        if (tx.Sender != pot.OwnerUserAddress)
                        {
                            tx.State = TransactionState.ChargeNotFromOwner;
                        }
                        else if (pot.Charged != null)
                        {
                            tx.State = TransactionState.ChargeAlreadyDone;
                        }
                        else if (tx.Amount < pot.InitialSize)
                        {
                            tx.State = TransactionState.ChargeTooSmall;
                        }
                        else
                        {
                            tx.State = TransactionState.ChargeOk;
                            pot.Charged = tx.Time;
                            charged = true;
                        }

                        if (pot.Stolen == null)
                        {
                            pot.TotalSize += tx.Amount;
                        }

                        db.Update(tx);
                        db.Update(pot);

                        if (charged)
                        {
                            db.Insert(ScheduledMessage.Create(pot.Id, ScheduledMessageType.ReferralRichMessage, null));
                        }

                        break;

                    case TransactionOpcode.Bet:
                        var (started, accepted, declined, stolen) = (false, false, false, false);
                        if (pot.Charged == null)
                        {
                            tx.State = TransactionState.BetBeforeCharge;
                        }
                        else if (pot.Stolen != null)
                        {
                            tx.State = TransactionState.BetAfterStolen;
                        }
                        else if (pot.LastTx != null && pot.LastTx.Value.Add(pot.Countdown) < tx.Time)
                        {
                            tx.State = TransactionState.BetAfterStolen;
                            pot.Stolen = pot.LastTx.Value.Add(pot.Countdown);
                            stolen = true;
                        }
                        else if (tx.Amount < pot.TxSizeNext)
                        {
                            tx.State = TransactionState.BetTooSmall;
                            declined = true;
                        }
                        else
                        {
                            tx.State = TransactionState.BetOk;
                            accepted = true;

                            if (pot.FirstTx is null)
                            {
                                pot.FirstTx = tx.Time;
                                started = true;
                            }

                            if (pot.TxSizeIncrease > 0)
                            {
                                pot.TxSizeNext += Math.Round(pot.TxSizeNext * pot.TxSizeIncrease / 100, 2);
                            }

                            pot.LastTx = tx.Time;
                            pot.TxCount++;
                        }

                        if (pot.Stolen == null)
                        {
                            pot.TotalSize += tx.Amount;
                        }

                        db.Update(tx);
                        db.Update(pot);

                        if (declined && tx.UserId != null)
                        {
                            db.Insert(ScheduledMessage.Create(pot.Id, ScheduledMessageType.PotTransactionDeclined, tx.UserId));
                        }

                        if (accepted && tx.UserId != null)
                        {
                            db.Insert(ScheduledMessage.Create(pot.Id, ScheduledMessageType.PotTransactionAccepted, tx.UserId));
                        }

                        if (started)
                        {
                            db.Insert(ScheduledMessage.Create(pot.Id, ScheduledMessageType.PotStarted, pot.OwnerUserId));
                        }

                        break;

                    default:
                        tx.State = TransactionState.SkippedUnknown;

                        if (pot.Stolen == null)
                        {
                            pot.TotalSize += tx.Amount;
                        }

                        db.Update(tx);
                        db.Update(pot);

                        break;
                }
            }

            pot.Touch();
            db.Update(pot);

            return true;
        }

        public bool CheckForStolen(Pot pot)
        {
            if (pot.Stolen == null && pot.LastTx != null && pot.LastTx.Value.Add(pot.Countdown) < DateTimeOffset.UtcNow)
            {
                pot.Stolen = pot.LastTx.Value.Add(pot.Countdown);
                pot.Touch();
                dbProvider.MainDb.Update(pot);
                return true;
            }

            return false;
        }
    }
}
