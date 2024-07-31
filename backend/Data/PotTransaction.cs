namespace MagicPot.Backend.Data
{
    using SQLite;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public class PotTransaction
    {
        [PrimaryKey]
        [AutoIncrement]
        public long Id { get; set; }

        [NotNull]
        [Indexed("Pot_State", 1)]
        public long PotId { get; set; }

        [NotNull]
        [Indexed("Pot_State", 2)]
        public PotTransactionState State { get; set; }

        [NotNull]
        [Indexed(Unique = true)]
        [MaxLength(100)]
        public string Hash { get; set; }

        [NotNull]
        public DateTimeOffset Notified { get; set; }

        [MaxLength(DbProvider.MaxLenAddress)]
        public string? Sender { get; set; }

        public decimal Amount { get; set; }

        public long? UserId { get; set; }

        [MaxLength(DbProvider.MaxLenAddress)]
        public string? Referrer { get; set; }
    }
}
