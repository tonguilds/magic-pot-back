namespace MagicPot.Backend.Services.Indexer
{
    using MagicPot.Backend.Data;
    using Xunit;

    public class PotUpdateTaskChargeTests : PotUpdateTaskBase
    {
        private Transaction tx;

        public PotUpdateTaskChargeTests()
        {
            pot.Charged = null;
            dbProvider.MainDb.Update(pot);

            tx = new Transaction()
            {
                PotId = pot.Id,
                Hash = Guid.NewGuid().ToString(),
                Sender = pot.OwnerUserAddress,
                Time = DateTimeOffset.UtcNow.AddMinutes(-1),
                IsJettonTransfer = true,
                Amount = pot.InitialSize,
                OpCode = TransactionOpcode.PrizeTransfer,
                State = TransactionState.Unprocessed,
                UserId = 12345,
            };
        }

        [Fact]
        public void ItWorks()
        {
            dbProvider.MainDb.Insert(tx);

            var oldSize = pot.TotalSize;

            Assert.True(task.ProcessNewTransactions(pot));

            Assert.Equal(oldSize + tx.Amount, pot.TotalSize);

            tx = dbProvider.MainDb.Get<Transaction>(tx.Id);
            Assert.Equal(TransactionState.ChargeOk, tx.State);

            // and throw msg to channel
            Assert.Single(dbProvider.MainDb.Table<ScheduledMessage>());
            Assert.Contains(dbProvider.MainDb.Table<ScheduledMessage>(), x => x.Type == ScheduledMessageType.ReferralRichMessage && x.UserId == null);
        }

        [Fact]
        public void NotAcceptFromNotCreator()
        {
            tx.Sender = "---";
            dbProvider.MainDb.Insert(tx);

            var oldSize = pot.TotalSize;

            Assert.True(task.ProcessNewTransactions(pot));

            // must add to pool anyway
            Assert.Equal(oldSize + tx.Amount, pot.TotalSize);

            tx = dbProvider.MainDb.Get<Transaction>(tx.Id);
            Assert.Equal(TransactionState.ChargeNotFromOwner, tx.State);

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
            Assert.Equal(TransactionState.ChargeTooSmall, tx.State);

            Assert.Empty(dbProvider.MainDb.Table<ScheduledMessage>());
        }

        [Fact]
        public void AcceptGreaterValue()
        {
            tx.Amount += 1;
            dbProvider.MainDb.Insert(tx);

            var oldSize = pot.TotalSize;

            Assert.True(task.ProcessNewTransactions(pot));

            Assert.Equal(oldSize + tx.Amount, pot.TotalSize);

            tx = dbProvider.MainDb.Get<Transaction>(tx.Id);
            Assert.Equal(TransactionState.ChargeOk, tx.State);

            // and throw msg to channel
            Assert.Single(dbProvider.MainDb.Table<ScheduledMessage>());
            Assert.Contains(dbProvider.MainDb.Table<ScheduledMessage>(), x => x.Type == ScheduledMessageType.ReferralRichMessage && x.UserId == null);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void NotAcceptIfAlreadyCharged(bool alreadyStolen)
        {
            pot.Charged = DateTimeOffset.UtcNow.AddHours(-1);
            pot.Stolen = alreadyStolen ? DateTimeOffset.UtcNow.AddMinutes(-1) : null;
            dbProvider.MainDb.Insert(tx);

            var oldSize = pot.TotalSize;

            Assert.True(task.ProcessNewTransactions(pot));

            // must add to pool only if not yet stolen
            if (alreadyStolen)
            {
                Assert.Equal(oldSize, pot.TotalSize);
            }
            else
            {
                Assert.Equal(oldSize + tx.Amount, pot.TotalSize);
            }

            tx = dbProvider.MainDb.Get<Transaction>(tx.Id);
            Assert.Equal(TransactionState.ChargeAlreadyDone, tx.State);

            Assert.Empty(dbProvider.MainDb.Table<ScheduledMessage>());
        }

        [Fact]
        public void NotAcceptBet()
        {
            tx.OpCode = TransactionOpcode.Bet;
            dbProvider.MainDb.Insert(tx);

            var oldSize = pot.TotalSize;

            Assert.True(task.ProcessNewTransactions(pot));

            // must add to pool anyway
            Assert.Equal(oldSize + tx.Amount, pot.TotalSize);

            tx = dbProvider.MainDb.Get<Transaction>(tx.Id);
            Assert.Equal(TransactionState.BetBeforeCharge, tx.State);

            Assert.Empty(dbProvider.MainDb.Table<ScheduledMessage>());
        }
    }
}
