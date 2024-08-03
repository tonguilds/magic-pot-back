namespace MagicPot.Backend.Data
{
    using SQLite;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public class Transaction
    {
        [PrimaryKey]
        [AutoIncrement]
        public long Id { get; set; }

        [NotNull]
        [Indexed("Pot_State", 1)]
        public long PotId { get; set; }

        [NotNull]
        [Indexed(Unique = true)]
        [MaxLength(100)]
        public string Hash { get; set; }

        [NotNull]
        public DateTimeOffset Time { get; set; }

        [NotNull]
        public decimal Amount { get; set; }

        [NotNull]
        [MaxLength(DbProvider.MaxLenAddress)]
        public string Sender { get; set; }

        [NotNull]
        public bool IsJettonTransfer { get; set; }

        [NotNull]
        public TransactionOpcode OpCode { get; set; }

        [NotNull]
        [Indexed("Pot_State", 2)]
        public TransactionState State { get; set; }

        public long? UserId { get; set; }

        [MaxLength(DbProvider.MaxLenAddress)]
        public string? Referrer { get; set; }
    }
}
