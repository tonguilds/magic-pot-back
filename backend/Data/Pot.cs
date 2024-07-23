namespace MagicPot.Backend.Data
{
    using SQLite;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public class Pot
    {
        [PrimaryKey]
        public long Id { get; set; }

        [NotNull]
        [Indexed(Unique = true)]
        [MaxLength(DbProvider.MaxLenKey)]
        public string Key { get; set; }

        [NotNull]
        [MaxLength(DbProvider.MaxLen255)]
        public string Name { get; set; }

        /// <summary>
        /// In non-bounceable form.
        /// </summary>
        [NotNull]
        [MaxLength(DbProvider.MaxLenAddress)]
        public string Address { get; set; }

        [NotNull]
        [MaxLength(DbProvider.MaxLenAddress)]
        public string Mnemonic { get; set; }

        [NotNull]
        [Indexed]
        public long OwnerUserId { get; set; }

        /// <summary>
        /// In Bounceable form.
        /// </summary>
        [NotNull]
        [MaxLength(DbProvider.MaxLenAddress)]
        public string OwnerUserAddress { get; set; }

        /// <summary>
        /// In bounceable form.
        /// </summary>
        [NotNull]
        [MaxLength(DbProvider.MaxLenAddress)]
        public string JettonMaster { get; set; }

        /// <summary>
        /// In bounceable form.
        /// </summary>
        [MaxLength(DbProvider.MaxLenAddress)]
        public string? JettonWallet { get; set; }

        [MaxLength(DbProvider.MaxLenUri)]
        public string? CoverImage { get; set; }

        public bool CoverIsAnimated { get; set; }

        [NotNull]
        public decimal InitialSize { get; set; }

        [NotNull]
        public decimal TotalSize { get; set; }

        [NotNull]
        public PotState State { get; set; }

        [NotNull]
        public DateTimeOffset Touched { get; set; }

        [NotNull]
        public DateTimeOffset NextUpdate { get; set; }

        [NotNull]
        public long SyncLt { get; set; }

        [NotNull]
        public DateTimeOffset SyncUtime { get; set; }

        [NotNull]
        public DateTimeOffset Created { get; set; }

        public DateTimeOffset? Charged { get; set; }

        public DateTimeOffset? FirstTx { get; set; }

        public DateTimeOffset? LastTx { get; set; }

        public DateTimeOffset? Stolen { get; set; }

        public DateTimeOffset? Paid { get; set; }

        [NotNull]
        public TimeSpan Countdown { get; set; }

        [NotNull]
        public decimal TxSizeNext { get; set; }

        [NotNull]
        public uint TxSizeIncrease { get; set; }

        [NotNull]
        public uint CreatorPercent { get; set; }

        [NotNull]
        public uint LastTxPercent { get; set; }

        [NotNull]
        public uint LastTxCount { get; set; }

        [NotNull]
        public uint RandomTxPercent { get; set; }

        [NotNull]
        public uint RandomTxCount { get; set; }

        [NotNull]
        public uint ReferralsPercent { get; set; }

        [NotNull]
        public uint BurnPercent { get; set; }

        public void Touch()
        {
            Touched = DateTimeOffset.UtcNow;
            UpdateNextUpdate();
        }

        public void UpdateNextUpdate()
        {
            var now = DateTimeOffset.UtcNow;
            var age = now.Subtract(Touched);
            if (age < TimeSpan.FromMinutes(1))
            {
                NextUpdate = now.AddSeconds(15);
            }
            else if (age < TimeSpan.FromMinutes(3))
            {
                NextUpdate = now.AddSeconds(7);
            }
            else if (age < TimeSpan.FromMinutes(10))
            {
                NextUpdate = now.AddSeconds(19);
            }
            else if (age < TimeSpan.FromHours(1))
            {
                NextUpdate = now.AddSeconds(42);
            }
            else
            {
                NextUpdate = now.AddMinutes(42);
            }
        }
    }
}
