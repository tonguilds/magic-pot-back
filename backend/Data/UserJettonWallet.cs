namespace MagicPot.Backend.Data
{
    using SQLite;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public class UserJettonWallet
    {
        [PrimaryKey]
        [AutoIncrement]
        public long Id { get; set; }

        [NotNull]
        [Indexed("UserJetton", 1, Unique = true)]
        [MaxLength(DbProvider.MaxLenAddress)]
        public string MainWallet { get; set; }

        [NotNull]
        [Indexed("UserJetton", 2, Unique = true)]
        [MaxLength(DbProvider.MaxLenAddress)]
        public string JettonMaster { get; set; }

        [Indexed]
        [MaxLength(DbProvider.MaxLenAddress)]
        public string? JettonWallet { get; set; }

        public decimal? Balance { get; set; }

        [NotNull]
        public DateTimeOffset Created { get; set; } = DateTimeOffset.UtcNow;

        [NotNull]
        public DateTimeOffset Updated { get; set; } = DateTimeOffset.UtcNow;
    }
}
