namespace MagicPot.Backend.Data
{
    using SQLite;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public class Jetton
    {
        [PrimaryKey]
        [AutoIncrement]
        public int Id { get; set; }

        /// <summary>
        /// Jetton master contract address (in bounceable mode).
        /// </summary>
        [NotNull]
        [MaxLength(DbProvider.MaxLenAddress)]
        public string Address { get; set; }

        [NotNull]
        [MaxLength(DbProvider.MaxLen255)]
        public string Name { get; set; }

        [NotNull]
        [MaxLength(DbProvider.MaxLen255)]
        public string Symbol { get; set; }

        [NotNull]
        public byte Decimals { get; set; }

        [MaxLength(DbProvider.MaxLenUri)]
        public string? Image { get; set; }

        public string? Description { get; set; }
    }
}
