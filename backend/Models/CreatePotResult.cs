namespace MagicPot.Backend.Models
{
    public class CreatePotResult
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
        /// Key of newly created pot (when there were no errors and wallet had been validated sucessfully).
        /// </summary>
        public string? Key { get; set; } = string.Empty;

        /// <summary>
        /// Transaction information to send prize (when there were no errors and wallet had been validated sucessfully).
        /// </summary>
        public TonConnectTransactionInfo? TransactionInfo { get; set; }
    }
}
