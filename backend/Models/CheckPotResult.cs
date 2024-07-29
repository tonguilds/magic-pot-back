namespace MagicPot.Backend.Models
{
    public class CheckPotResult
    {
        /// <summary>
        /// Validation errors, if any.
        /// </summary>
        public IDictionary<string, string[]>? Errors { get; set; }

        /// <summary>
        /// <b>True</b> while jetton wallet is being validated.
        /// </summary>
        public bool IsValidatingWallet { get; set; }
    }
}
