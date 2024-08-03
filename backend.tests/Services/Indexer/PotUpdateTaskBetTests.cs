namespace MagicPot.Backend.Services.Indexer
{
    using MagicPot.Backend.Data;
    using Xunit;

    public class PotUpdateTaskBetTests : PotUpdateTaskBase
    {
        private Transaction tx;

        public PotUpdateTaskBetTests()
        {
            pot.Charged = DateTimeOffset.UtcNow.AddMinutes(-20);
            pot.Countdown = TimeSpan.FromMinutes(5);
            dbProvider.MainDb.Update(pot);

            tx = new Transaction()
            {
                PotId = pot.Id,
                Hash = Guid.NewGuid().ToString(),
                Sender = pot.OwnerUserAddress,
                Time = DateTimeOffset.UtcNow.AddMinutes(-1),
                IsJettonTransfer = true,
                Amount = pot.TxSizeNext,
                OpCode = TransactionOpcode.Bet,
                State = TransactionState.Unprocessed,
                UserId = 12345,
            };
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ItWorks(bool alreadyStarted)
        {
            pot.FirstTx = alreadyStarted ? DateTimeOffset.UtcNow.AddMinutes(-1) : null;
            pot.LastTx = pot.FirstTx;
            dbProvider.MainDb.Insert(tx);

            var oldSize = pot.TotalSize;

            Assert.True(task.ProcessNewTransactions(pot));

            Assert.Equal(oldSize + tx.Amount, pot.TotalSize);

            tx = dbProvider.MainDb.Get<Transaction>(tx.Id);
            Assert.Equal(TransactionState.BetOk, tx.State);
            Assert.Equal(tx.Time, pot.LastTx);

            // and throw some msgs
            Assert.Contains(dbProvider.MainDb.Table<ScheduledMessage>(), x => x.Type == ScheduledMessageType.PotTransactionAccepted && x.UserId == tx.UserId);

            if (!alreadyStarted)
            {
                Assert.Equal(tx.Time, pot.FirstTx);
                Assert.Contains(dbProvider.MainDb.Table<ScheduledMessage>(), x => x.Type == ScheduledMessageType.PotStarted && x.UserId == pot.OwnerUserId);
            }
        }

        [Fact]
        public void NotAcceptBeforeCharge()
        {
            pot.Charged = null;
            dbProvider.MainDb.Insert(tx);

            var oldSize = pot.TotalSize;

            Assert.True(task.ProcessNewTransactions(pot));

            // must add to pool anyway
            Assert.Equal(oldSize + tx.Amount, pot.TotalSize);

            tx = dbProvider.MainDb.Get<Transaction>(tx.Id);
            Assert.Equal(TransactionState.BetBeforeCharge, tx.State);

            Assert.Empty(dbProvider.MainDb.Table<ScheduledMessage>());
        }

        [Fact]
        public void NotAcceptWhenAlreadyStolen()
        {
            pot.Stolen = DateTimeOffset.UtcNow.AddSeconds(-1);
            dbProvider.MainDb.Insert(tx);

            var oldSize = pot.TotalSize;

            Assert.True(task.ProcessNewTransactions(pot));

            // must NOT add to pool
            Assert.Equal(oldSize, pot.TotalSize);

            tx = dbProvider.MainDb.Get<Transaction>(tx.Id);
            Assert.Equal(TransactionState.BetAfterStolen, tx.State);

            Assert.Empty(dbProvider.MainDb.Table<ScheduledMessage>());
        }

        [Fact]
        public void NotAcceptWhenLate()
        {
            pot.Stolen = null;
            pot.Countdown = TimeSpan.FromMinutes(20);
            pot.LastTx = DateTimeOffset.UtcNow.AddMinutes(-30);
            dbProvider.MainDb.Insert(tx);

            var oldSize = pot.TotalSize;

            Assert.True(task.ProcessNewTransactions(pot));

            // must NOT add to pool
            Assert.Equal(oldSize, pot.TotalSize);

            tx = dbProvider.MainDb.Get<Transaction>(tx.Id);
            Assert.Equal(TransactionState.BetAfterStolen, tx.State);

            // pool must be set to Stolen
            Assert.NotNull(pot.Stolen);

            Assert.Empty(dbProvider.MainDb.Table<ScheduledMessage>());
        }

        [Fact]
        public void NotAcceptSmallerValue()
        {
            tx.Amount -= 1;
            dbProvider.MainDb.Insert(tx);

            var oldSize = pot.TotalSize;

            Assert.True(task.ProcessNewTransactions(pot));

            // must add to pool anyway
            Assert.Equal(oldSize + tx.Amount, pot.TotalSize);

            tx = dbProvider.MainDb.Get<Transaction>(tx.Id);
            Assert.Equal(TransactionState.BetTooSmall, tx.State);

            Assert.Single(dbProvider.MainDb.Table<ScheduledMessage>());
            Assert.Contains(dbProvider.MainDb.Table<ScheduledMessage>(), x => x.Type == ScheduledMessageType.PotTransactionDeclined && x.UserId == tx.UserId);
        }

        [Fact]
        public void AcceptGreaterValue()
        {
            pot.FirstTx = DateTimeOffset.UtcNow.AddMinutes(-5);
            pot.LastTx = pot.FirstTx;
            tx.Amount += 1;
            dbProvider.MainDb.Insert(tx);

            var oldSize = pot.TotalSize;

            Assert.True(task.ProcessNewTransactions(pot));

            Assert.Equal(oldSize + tx.Amount, pot.TotalSize);

            tx = dbProvider.MainDb.Get<Transaction>(tx.Id);
            Assert.Equal(TransactionState.BetOk, tx.State);

            Assert.Single(dbProvider.MainDb.Table<ScheduledMessage>());
            Assert.Contains(dbProvider.MainDb.Table<ScheduledMessage>(), x => x.Type == ScheduledMessageType.PotTransactionAccepted && x.UserId == tx.UserId);
        }

        [Fact]
        public void IncreaseNextTxWhenNeeded()
        {
            pot.TxSizeIncrease = 25;
            dbProvider.MainDb.Insert(tx);

            Assert.Equal(0, pot.TotalSize);

            Assert.True(task.ProcessNewTransactions(pot));

            // must add to pool anyway
            Assert.Equal(tx.Amount, pot.TotalSize);

            tx = dbProvider.MainDb.Get<Transaction>(tx.Id);
            Assert.Equal(TransactionState.BetOk, tx.State);

            Assert.Equal(tx.Amount * 1.25M, pot.TxSizeNext);
        }
    }
}
