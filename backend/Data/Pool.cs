namespace MagicPot.Backend.Data
{
    using SQLite;

    public class Pool
    {
        [PrimaryKey]
        public long Id { get; set; }

        [NotNull]
        public string Address { get; set; } = string.Empty;

        [NotNull]
        public PoolState State { get; set; }

        [NotNull]
        [Indexed]
        public long OwnerId { get; set; }
    }
}
