namespace MagicPot.Backend.Models
{
    using System.ComponentModel.DataAnnotations;
    using MagicPot.Backend.Attributes;
    using MagicPot.Backend.Data;

    public class CreateTransactionModel
    {
        /// <summary>
        /// Pot key.
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Address of main user wallet (connected in Ton Connect).
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        [MaxLength(DbProvider.MaxLenAddress)]
        [TonAddress]
        public string UserAddress { get; set; } = string.Empty;

        /// <summary>
        /// Referrer address (if any).
        /// </summary>
        [TonAddress]
        public string? Referrer { get; set; }

        /// <summary>
        /// Amount of tokens to send. Optional (pot required min.size will be used).
        /// Useful for pots with increasing tx size, when user may want to send more tokens to keep his transaction "valid" even if transactions of other users arrive before his one.
        /// </summary>
        [Range(1, int.MaxValue)]
        public decimal? Amount { get; set; }
    }
}
