namespace MagicPot.Backend.Controllers
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using MagicPot.Backend.Attributes;
    using MagicPot.Backend.Data;

    public class NewPotModel : IValidatableObject
    {
        /// <summary>
        /// Address of main user wallet (connected in Ton Connect).
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        [MaxLength(DbProvider.MaxLenAddress)]
        [TonAddress]
        public string UserAddress { get; set; } = string.Empty;

        /// <summary>
        /// Pot name.
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        [MaxLength(DbProvider.MaxLenName)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Token address (address of Jetton Master contract).
        /// </summary>
        /// <remarks>
        /// <para>Either Token Name, or Token Address must be specified, not both.</para>
        /// </remarks>
        [Required(AllowEmptyStrings = false)]
        [MaxLength(DbProvider.MaxLenAddress)]
        [TonAddress]
        public string TokenAddress { get; set; } = string.Empty;

        /// <summary>
        /// Initial pot size.
        /// </summary>
        [Required]
        [Range(1, 1_000_000_000, ConvertValueInInvariantCulture = true)]
        public uint InitialSize { get; set; }

        /// <summary>
        /// Countdown time (in minutes).
        /// </summary>
        [Required]
        [Range(1, 5999, ConvertValueInInvariantCulture = true)] // max 99H 59min
        public uint CountdownTimerMinutes { get; set; }

        /// <summary>
        /// Transaction size.
        /// </summary>
        [Required]
        [Range(1, 1_000_000_000, ConvertValueInInvariantCulture = true)]
        public decimal TransactionSize { get; set; }

        /// <summary>
        /// Transaction grow ratio for 'increasing' (in percents), or 0 (zero) for 'fixed'.
        /// </summary>
        [Range(0, 9999, ConvertValueInInvariantCulture = true)]
        public uint IncreasingTransactionPercentage { get; set; }

        /// <summary>
        /// Percentage of prize that creator will receive, or 0 (zero) to recieve nothing.
        /// </summary>
        [Range(0, 100, ConvertValueInInvariantCulture = true)]
        public uint? CreatorPercent { get; set; }

        /// <summary>
        /// Percentage of prize that last X of transactions will share, or 0 (zero) to share nothing.
        /// </summary>
        [Range(0, 100, ConvertValueInInvariantCulture = true)]
        public uint? LastTransactionsPercent { get; set; }

        /// <summary>
        /// Number of last transactions that will share prize. Must be set only when <see cref="LastTransactionsPercent">LastTransactionsPercent</see> is set.
        /// </summary>
        [Range(0, 99, ConvertValueInInvariantCulture = true)]
        public uint? LastTransactionsCount { get; set; }

        /// <summary>
        /// Percentage of prize that X of random transactions will share, or 0 (zero) to share nothing.
        /// </summary>
        [Range(0, 100, ConvertValueInInvariantCulture = true)]
        public uint? RandomTransactionsPercent { get; set; }

        /// <summary>
        /// Number of random transactions that will share prize. Must be set only when <see cref="RandomTransactionsPercent">RandomTransactionsPercent</see> is set.
        /// </summary>
        [Range(0, 99, ConvertValueInInvariantCulture = true)]
        public uint? RandomTransactionsCount { get; set; }

        /// <summary>
        /// Percentage of prize that referrals of winners will receive, or 0 (zero) to recieve nothing.
        /// </summary>
        [Range(0, 100, ConvertValueInInvariantCulture = true)]
        public uint? ReferralsPercent { get; set; }

        /// <summary>
        /// Percentage of prize that will be burned, or 0 (zero) to burn nothing.
        /// </summary>
        [Range(0, 100, ConvertValueInInvariantCulture = true)]
        public uint? BurnPercent { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var sharesSum = CreatorPercent + LastTransactionsCount + RandomTransactionsPercent + ReferralsPercent + BurnPercent;
            var sharesOk = sharesSum == 100;

            if (!sharesOk)
            {
                if (CreatorPercent > 0 || sharesSum == 0)
                {
                    yield return new ValidationResult(Messages.SumOfSharesMustBe100, [nameof(CreatorPercent)]);
                }

                if (LastTransactionsPercent > 0 || sharesSum == 0)
                {
                    yield return new ValidationResult(Messages.SumOfSharesMustBe100, [nameof(LastTransactionsPercent)]);
                }

                if (RandomTransactionsPercent > 0 || sharesSum == 0)
                {
                    yield return new ValidationResult(Messages.SumOfSharesMustBe100, [nameof(RandomTransactionsPercent)]);
                }

                if (ReferralsPercent > 0 || sharesSum == 0)
                {
                    yield return new ValidationResult(Messages.SumOfSharesMustBe100, [nameof(ReferralsPercent)]);
                }

                if (BurnPercent > 0 || sharesSum == 0)
                {
                    yield return new ValidationResult(Messages.SumOfSharesMustBe100, [nameof(BurnPercent)]);
                }
            }

            if (LastTransactionsPercent > 0 && LastTransactionsCount == 0)
            {
                yield return new ValidationResult(Messages.MustBeNonZero, [nameof(LastTransactionsCount)]);
            }

            if (RandomTransactionsPercent > 0 && RandomTransactionsCount == 0)
            {
                yield return new ValidationResult(Messages.MustBeNonZero, [nameof(RandomTransactionsCount)]);
            }
        }
    }
}
