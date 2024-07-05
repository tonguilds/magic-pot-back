namespace MagicPot.Backend.Data
{
    using SQLite;

    public class UserJettonWallet
    {
        [PrimaryKey]
        [AutoIncrement]
        public long Id { get; set; }

        [NotNull]
        [Indexed("UserJetton", 1, Unique = true)]
        public string MainWallet { get; set; } = string.Empty;

        [NotNull]
        [Indexed("UserJetton", 2, Unique = true)]
        public string JettonMaster { get; set; } = string.Empty;

        [Indexed]
        public string? JettonWallet { get; set; }

        [NotNull]
        public DateTimeOffset Created { get; set; } = DateTimeOffset.UtcNow;
    }
}
