namespace MagicPot.Backend.Services.Indexer
{
    using MagicPot.Backend.Data;
    using MagicPot.Backend.Utils;
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

        protected async Task<bool> Update(Pot pot)
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

        protected async Task<bool> UpdatePotJettonAddress(Pot pot)
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

        protected async Task<bool> LoadNewTransactions(Pot pot, TransactionId lastTransaction, DateTimeOffset syncTime, Jetton jetton)
        {
            var found = false;
            var db = dbProvider.MainDb;

            await foreach (var tx in blockchainReader.EnumerateTransactions(pot.Address, lastTransaction, pot.SyncLt))
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
                    || !blockchainReader.TryParseJettonTransferNotification(tx.InMsg, out var jettonWalletAddress, out var queryId, out var userWalletAddres, out var amount, out var encodedPotId, out var encodedUserId, out var encodedReferrerAddress))
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

                if (encodedPotId != pot.Id)
                {
                    ptx.State = PotTransactionState.ManualTransfer;
                    db.Insert(ptx);
                    logger.LogDebug("Pot {Key} tx {Hash} at {Time} is a MANUAL jetton transfer, ignored", pot.Key, ptx.Hash, ptx.Notified);
                    continue;
                }

                ptx.State = PotTransactionState.Processing;
                ptx.Sender = userWalletAddres;
                ptx.Amount = (decimal)amount / (decimal)Math.Pow(10, jetton.Decimals);
                ptx.UserId = encodedUserId;
                ptx.Referrer = encodedReferrerAddress;
                db.Insert(ptx);
                logger.LogInformation("Pot {Key} tx {Hash} at {Time} found new jetton transfer (user {Address})", pot.Key, ptx.Hash, ptx.Notified, ptx.Sender);
                found = true;
            }

            pot.SyncLt = lastTransaction.Lt;
            pot.SyncUtime = syncTime;
            pot.Touch();
            db.Update(pot);
            logger.LogInformation("Pot {Key} updated to lt={Lt}, sync {Time}", pot.Key, pot.SyncLt, pot.SyncUtime);

            return found;
        }

        protected bool ProcessNewTransactions(Pot pot)
        {
            var db = dbProvider.MainDb;

            var list = db.Table<PotTransaction>()
                .Where(x => x.PotId == pot.Id && x.State == PotTransactionState.Processing)
                .OrderBy(x => x.Notified)
                .ToList();

            if (list.Count == 0)
            {
                return false;
            }

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
                else if (pot.Stolen != null)
                {
                    tx.State = PotTransactionState.AfterStolen;
                    db.Update(tx);
                }
                else if (pot.LastTx != null && pot.LastTx.Value.Add(pot.Countdown) < tx.Notified)
                {
                    tx.State = PotTransactionState.AfterStolen;
                    db.Update(tx);

                    pot.Stolen = pot.LastTx.Value.Add(pot.Countdown);
                }
                else if (tx.Amount < pot.TxSizeNext)
                {
                    tx.State = PotTransactionState.TooSmallForBet;
                    db.Update(tx);

                    if (tx.UserId != null)
                    {
                        db.Insert(ScheduledMessage.Create(pot.Id, ScheduledMessageType.PotTransactionDeclined, tx.UserId));
                    }
                }
                else
                {
                    tx.State = PotTransactionState.Bet;
                    db.Update(tx);

                    if (tx.UserId != null)
                    {
                        db.Insert(ScheduledMessage.Create(pot.Id, ScheduledMessageType.PotTransactionAccepted, tx.UserId));
                    }

                    if (pot.FirstTx is null)
                    {
                        pot.FirstTx = tx.Notified;
                        db.Insert(ScheduledMessage.Create(pot.Id, ScheduledMessageType.PotStarted, pot.OwnerUserId));
                    }

                    if (pot.TxSizeIncrease > 0)
                    {
                        pot.TxSizeNext += Math.Round(pot.TxSizeNext * pot.TxSizeIncrease / 100, 2);
                    }

                    pot.LastTx = tx.Notified;
                    pot.TxCount++;
                }

                pot.TotalSize += tx.Amount;
                pot.Touch();
                db.Update(pot);
            }

            return true;
        }

        protected bool CheckForStolen(Pot pot)
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
