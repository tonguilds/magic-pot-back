namespace MagicPot.Backend.Data
{
    using SQLite;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public class PrecachedMnemonic
    {
        [PrimaryKey]
        [MaxLength(DbProvider.MaxLenAddress)]
        public string Address { get; set; }

        [NotNull]
        [MaxLength(24 * 100)]
        public string Mnemonic { get; set; }
    }
}
