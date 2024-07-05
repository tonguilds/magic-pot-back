namespace MagicPot.Backend.Controllers
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using MagicPot.Backend.Attributes;

    public class NewPotModel : IValidatableObject
    {
        /// <summary>
        /// Address of main user wallet (connected in Ton Connect).
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        [MaxLength(100)]
        [TonAddress]
        public string UserAddress { get; set; } = string.Empty;

        /// <summary>
        /// Pot name.
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        [MaxLength(60)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Token name (from predefined list of well-known tokens).
        /// </summary>
        /// <remarks>
        /// <para>Either Token Name, or Token Address must be specified, not both.</para>
        /// </remarks>
        [MaxLength(100)]
        public string? TokenName { get; set; }

        /// <summary>
        /// Token address (address of Jetton Master contract).
        /// </summary>
        /// <remarks>
        /// <para>Either Token Name, or Token Address must be specified, not both.</para>
        /// </remarks>
        [MaxLength(100)]
        [TonAddress]
        public string? TokenAddress { get; set; }

        /// <summary>
        /// Initial pot size.
        /// </summary>
        [Required]
        [Range(1, 1_000_000_000, ConvertValueInInvariantCulture = true)]
        public int InitialSize { get; set; }

        /// <summary>
        /// Countdown time (in minutes).
        /// </summary>
        [Required]
        [Range(1, 60 * 24, ConvertValueInInvariantCulture = true)]
        public int CountdownTimerMinutes { get; set; }

        /// <summary>
        /// Transaction size.
        /// </summary>
        [Required]
        [Range(1, 1_000_000_000, ConvertValueInInvariantCulture = true)]
        public int TransactionSize { get; set; }

        /// <summary>
        /// Transaction grow ratio for 'increasing', or 0 (zero) for 'fixed'.
        /// </summary>
        [Range(0.1, 100, ConvertValueInInvariantCulture = true)]
        public decimal IncreasingTransactionPercentage { get; set; }

        /// <summary>
        /// Percentage of prize that winner will receive, or 0 (zero) to recieve nothing.
        /// </summary>
        public uint FinalTransactionPercent { get; set; }

        /// <summary>
        /// Percentage of prize that last X of transactions will share, or 0 (zero) to share nothing.
        /// </summary>
        public uint PreFinalTransactionsPercent { get; set; }

        /// <summary>
        /// Number of last transactions that will share prize. Must be set only when <see cref="PreFinalTransactionsPercent">PreFinalTransactionsPercent</see> is set.
        /// </summary>
        public uint PreFinalTransactionsCount { get; set; }

        /// <summary>
        /// Percentage of prize that referrals of winners will receive, or 0 (zero) to recieve nothing.
        /// </summary>
        public uint ReferralsPercent { get; set; }

        /// <summary>
        /// Percentage of prize that creator will receive, or 0 (zero) to recieve nothing.
        /// </summary>
        public uint CreatorPercent { get; set; }

        /// <summary>
        /// Percentage of prize that will be burned, or 0 (zero) to burn nothing.
        /// </summary>
        public uint BurnPercent { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var tokenOk = string.IsNullOrEmpty(TokenName) != string.IsNullOrEmpty(TokenAddress);
            if (!tokenOk)
            {
                yield return new ValidationResult(Messages.TokenNameOrAddressRequired, [nameof(TokenName)]);
                yield return new ValidationResult(Messages.TokenNameOrAddressRequired, [nameof(TokenAddress)]);
            }

            var totalOk = (FinalTransactionPercent + PreFinalTransactionsPercent + ReferralsPercent + CreatorPercent + BurnPercent) == 100;

            if (FinalTransactionPercent > 0)
            {
                if (FinalTransactionPercent > 100)
                {
                    yield return new ValidationResult(Messages.MaxAllowedValueIs100, [nameof(FinalTransactionPercent)]);
                }
                else if (!totalOk)
                {
                    yield return new ValidationResult(Messages.SumOfSharesMustBe100, [nameof(FinalTransactionPercent)]);
                }
            }

            if (PreFinalTransactionsPercent > 0)
            {
                if (PreFinalTransactionsPercent > 100)
                {
                    yield return new ValidationResult(Messages.MaxAllowedValueIs100, [nameof(PreFinalTransactionsPercent)]);
                }
                else if (!totalOk)
                {
                    yield return new ValidationResult(Messages.SumOfSharesMustBe100, [nameof(PreFinalTransactionsPercent)]);
                }

                if (PreFinalTransactionsCount == 0)
                {
                    yield return new ValidationResult(Messages.MustBeNonZero, [nameof(PreFinalTransactionsCount)]);
                }
            }
            else
            {
                if (PreFinalTransactionsCount > 0)
                {
                    yield return new ValidationResult(Messages.MustBeZero, [nameof(PreFinalTransactionsCount)]);
                }
            }

            if (ReferralsPercent > 0)
            {
                if (ReferralsPercent > 100)
                {
                    yield return new ValidationResult(Messages.MaxAllowedValueIs100, [nameof(ReferralsPercent)]);
                }
                else if (!totalOk)
                {
                    yield return new ValidationResult(Messages.SumOfSharesMustBe100, [nameof(ReferralsPercent)]);
                }
            }

            if (CreatorPercent > 0)
            {
                if (CreatorPercent > 100)
                {
                    yield return new ValidationResult(Messages.MaxAllowedValueIs100, [nameof(CreatorPercent)]);
                }
                else if (!totalOk)
                {
                    yield return new ValidationResult(Messages.SumOfSharesMustBe100, [nameof(CreatorPercent)]);
                }
            }

            if (BurnPercent > 0)
            {
                if (BurnPercent > 100)
                {
                    yield return new ValidationResult(Messages.MaxAllowedValueIs100, [nameof(BurnPercent)]);
                }
                else if (!totalOk)
                {
                    yield return new ValidationResult(Messages.SumOfSharesMustBe100, [nameof(BurnPercent)]);
                }
            }
        }
    }
}
