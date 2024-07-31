namespace MagicPot.Backend.Data
{
    using SQLite;

    public class User
    {
        [PrimaryKey]
        public long Id { get; set; }

        [MaxLength(DbProvider.MaxLen255)]
        public string? Username { get; set; }

        [MaxLength(DbProvider.MaxLen255)]
        public string? FirstName { get; set; }

        [MaxLength(DbProvider.MaxLen255)]
        public string? LastName { get; set; }

        [MaxLength(DbProvider.MaxLen255)]
        public string? LanguageCode { get; set; }

        public bool IsPremium { get; set; }

        public bool AllowsWriteToPM { get; set; }

        [NotNull]
        public DateTimeOffset Created { get; set; }

        [NotNull]
        public int Points { get; set; }
    }
}
