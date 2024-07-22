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
        [RegularExpression(@"[A-Za-zА-Яа-я\ \d\.\,\!\?]+")]
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
        [Range(1, (99 * 60) + 59, ConvertValueInInvariantCulture = true, ErrorMessageResourceType = typeof(Messages), ErrorMessageResourceName = nameof(Messages.MaxValueIs99h59m))]
        public uint CountdownTimerMinutes { get; set; }

        /// <summary>
        /// Transaction size.
        /// </summary>
        [Required]
        [Range(0.1, 1_000_000_000, ConvertValueInInvariantCulture = true)]
        public decimal TransactionSize { get; set; }

        /// <summary>
        /// Transaction grow ratio for 'increasing' (in percents), or null for 'fixed'.
        /// </summary>
        [Range(1, 9999, ConvertValueInInvariantCulture = true)]
        public uint? IncreasingTransactionPercentage { get; set; }

        /// <summary>
        /// Percentage of prize that creator will receive, or null to recieve nothing.
        /// </summary>
        [Range(1, 100, ConvertValueInInvariantCulture = true)]
        public uint? CreatorPercent { get; set; }

        /// <summary>
        /// Percentage of prize that last X of transactions will share, or null to share nothing.
        /// </summary>
        [Range(1, 100, ConvertValueInInvariantCulture = true)]
        public uint? LastTransactionsPercent { get; set; }

        /// <summary>
        /// Number of last transactions that will share prize. Must be set only when <see cref="LastTransactionsPercent">LastTransactionsPercent</see> is set.
        /// </summary>
        [Range(1, 99, ConvertValueInInvariantCulture = true)]
        public uint? LastTransactionsCount { get; set; }

        /// <summary>
        /// Percentage of prize that X of random transactions will share, or null to share nothing.
        /// </summary>
        [Range(1, 100, ConvertValueInInvariantCulture = true)]
        public uint? RandomTransactionsPercent { get; set; }

        /// <summary>
        /// Number of random transactions that will share prize. Must be set only when <see cref="RandomTransactionsPercent">RandomTransactionsPercent</see> is set.
        /// </summary>
        [Range(1, 99, ConvertValueInInvariantCulture = true)]
        public uint? RandomTransactionsCount { get; set; }

        /// <summary>
        /// Percentage of prize that referrals of winners will receive, or null to recieve nothing.
        /// </summary>
        [Range(1, 100, ConvertValueInInvariantCulture = true)]
        public uint? ReferralsPercent { get; set; }

        /// <summary>
        /// Percentage of prize that will be burned, or null to burn nothing.
        /// </summary>
        [Range(1, 100, ConvertValueInInvariantCulture = true)]
        public uint? BurnPercent { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var sharesSum = (CreatorPercent ?? 0) + (LastTransactionsPercent ?? 0) + (RandomTransactionsPercent ?? 0) + (ReferralsPercent ?? 0) + (BurnPercent ?? 0);
            var sharesOk = sharesSum == 100;

            if (!sharesOk)
            {
                if (CreatorPercent.HasValue || sharesSum == 0)
                {
                    yield return new ValidationResult(Messages.SumOfSharesMustBe100, [nameof(CreatorPercent)]);
                }

                if (LastTransactionsPercent.HasValue || sharesSum == 0)
                {
                    yield return new ValidationResult(Messages.SumOfSharesMustBe100, [nameof(LastTransactionsPercent)]);
                }

                if (RandomTransactionsPercent.HasValue || sharesSum == 0)
                {
                    yield return new ValidationResult(Messages.SumOfSharesMustBe100, [nameof(RandomTransactionsPercent)]);
                }

                if (ReferralsPercent.HasValue || sharesSum == 0)
                {
                    yield return new ValidationResult(Messages.SumOfSharesMustBe100, [nameof(ReferralsPercent)]);
                }

                if (BurnPercent.HasValue || sharesSum == 0)
                {
                    yield return new ValidationResult(Messages.SumOfSharesMustBe100, [nameof(BurnPercent)]);
                }
            }

            if (LastTransactionsPercent.HasValue != LastTransactionsCount.HasValue)
            {
                yield return new ValidationResult(Messages.ValuesInconsistentLastTransactions, [nameof(LastTransactionsCount)]);
            }

            if (RandomTransactionsPercent.HasValue != RandomTransactionsCount.HasValue)
            {
                yield return new ValidationResult(Messages.ValuesInconsistentRandomTransactions, [nameof(RandomTransactionsCount)]);
            }
        }
    }
}
