namespace MagicPot.Backend.Data
{
    using SQLite;

    public class Jetton
    {
        [PrimaryKey]
        [AutoIncrement]
        public int Id { get; set; }

        /// <summary>
        /// Jetton master contract address (in bounceable mode).
        /// </summary>
        [NotNull]
        public string Address { get; set; } = string.Empty;

        [NotNull]
        public string Name { get; set; } = string.Empty;

        [NotNull]
        public string Symbol { get; set; } = string.Empty;

        [NotNull]
        public byte Decimals { get; set; }

        public string? Image { get; set; }

        public string? Description { get; set; }
    }
}
