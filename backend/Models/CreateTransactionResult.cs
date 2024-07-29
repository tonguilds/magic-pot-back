namespace MagicPot.Backend.Models
{
    public class CreateTransactionResult
    {
        /// <summary>
        /// Validation errors, if any.
        /// </summary>
        public IDictionary<string, string[]>? Errors { get; set; }

        /// <summary>
        /// <b>True</b> while jetton wallet is being validated.
        /// </summary>
        public bool IsValidatingWallet { get; set; }

        /// <summary>
        /// Contains transaction information when there were no errors and wallet had been validated sucessfully.
        /// </summary>
        public TonConnectTransactionInfo? TransactionInfo { get; set; }
    }
}
