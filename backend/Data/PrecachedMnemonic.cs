namespace MagicPot.Backend.Data
{
    using SQLite;

    public class PrecachedMnemonic
    {
        [PrimaryKey]
        public string Address { get; set; } = string.Empty;

        [NotNull]
        public string Mnemonic { get; set; } = string.Empty;
    }
}
