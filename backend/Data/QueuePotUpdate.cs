namespace MagicPot.Backend.Data
{
    using SQLite;

    public class QueuePotUpdate
    {
        [PrimaryKey]
        [AutoIncrement]
        public long Id { get; set; }

        [NotNull]
        public long PotId { get; set; }

        [NotNull]
        [Indexed]
        public DateTimeOffset When { get; set; } = DateTimeOffset.UtcNow;
    }
}
