namespace MagicPot.Backend.Data
{
    using SQLite;

    public class Pot
    {
        [PrimaryKey]
        public long Id { get; set; }

        [NotNull]
        [Indexed(Unique = true)]
        public string Key { get; set; } = string.Empty;

        [NotNull]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// In non-bounceable form.
        /// </summary>
        [NotNull]
        public string Address { get; set; } = string.Empty;

        public string Mnemonic { get; set; } = string.Empty;

        [NotNull]
        [Indexed]
        public long OwnerUserId { get; set; }

        /// <summary>
        /// In Bounceable form.
        /// </summary>
        [NotNull]
        public string OwnerUserAddress { get; set; } = string.Empty;

        /// <summary>
        /// In bounceable form.
        /// </summary>
        [NotNull]
        public string TokenAddress { get; set; } = string.Empty;

        /// <summary>
        /// In bounceable form.
        /// </summary>
        [NotNull]
        public string JettonWalletAddress { get; set; } = string.Empty;

        [NotNull]
        public int InitialSize { get; set; }

        [NotNull]
        public DateTimeOffset Created { get; set; }
    }
}
