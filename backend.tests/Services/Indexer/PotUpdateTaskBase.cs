namespace MagicPot.Backend.Services.Indexer
{
    using MagicPot.Backend.Data;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Moq;
    using TonLibDotNet;
    using Xunit;

    public abstract class PotUpdateTaskBase
    {
        protected readonly Mock<ILogger<PotUpdateTask>> loggerMock = new(MockBehavior.Loose);
        protected readonly Mock<INotificationService> notificationServiceMock = new(MockBehavior.Strict);
        protected readonly Mock<ITonClient> tonClientMock = new(MockBehavior.Strict);

        protected readonly IDbProvider dbProvider;
        protected readonly PotUpdateTask task;
        protected Pot pot;

        protected PotUpdateTaskBase()
        {
            var opt = new Mock<IOptions<BackendOptions>>();
            opt.SetupGet(x => x.Value).Returns(new BackendOptions());

            dbProvider = new MemoryDbProvider(opt.Object, new Mock<ILogger<DbProvider>>(MockBehavior.Loose).Object);
            dbProvider.Migrate();

            pot = new Pot()
            {
                Id = 123,
                Key = "test",
                Name = "test",
                Address = "UQCcvfXb2-CjZem78cdELqEpw21hGGViEsSRlKGwo5ciUDn8", // random
                Mnemonic = "test",
                OwnerUserId = 19,
                InitialSize = 123,
                TxSizeNext = 19,
                OwnerUserAddress = "EQB3ncyBUTjZUA5EnFKR5_EnOMI9V1tTEAAPaiU71gc4TiUt", // random
                JettonMaster = "EQBWjPASSjsgibEv3fGUCwSwFyUxLVFaywZzNmuBXPFOFfOG", // random
                JettonWallet = "EQARULUYsmJq1RiZ-YiH-IJLcAZUVkVff-KBPwEmmaQGH6aC", // random
            };
            dbProvider.MainDb.Insert(pot);

            task = new(loggerMock.Object, dbProvider, new BlockchainReader(tonClientMock.Object), notificationServiceMock.Object);
        }

        [Fact]
        public void DoesNothingWithoutRows()
        {
            Assert.False(task.ProcessNewTransactions(pot));
        }

        [Fact]
        public void DoesNothingWithoutUnprocessedRows()
        {
            dbProvider.MainDb.Insert(new Transaction()
            {
                PotId = pot.Id,
                Hash = Guid.NewGuid().ToString(),
                Sender = Guid.NewGuid().ToString(),
                State = TransactionState.InvalidNoSender,
            });

            Assert.False(task.ProcessNewTransactions(pot));
        }
    }
}
