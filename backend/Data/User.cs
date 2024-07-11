namespace MagicPot.Backend.Data
{
    using SQLite;

    public class User
    {
        [PrimaryKey]
        public long Id { get; set; }

        [MaxLength(DbProvider.MaxLen255)]
        public string? Username { get; set; }

        [Indexed]
        public long? Referrer { get; set; }

        [NotNull]
        public DateTimeOffset Created { get; set; }

        [NotNull]
        public int Points { get; set; }
    }
}
