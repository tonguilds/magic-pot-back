namespace MagicPot.Backend.Data
{
    public enum PotTransactionState
    {
        /// <summary>
        /// Transaction is not a jetton transfer.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Transaction is a fake jetton tranfer (notification arrived not from our jetton wallet).
        /// </summary>
        FakeTransfer = 1,

        /// <summary>
        /// Transaction is fine, and waiting for additional classification.
        /// </summary>
        Processing = 2,

        /// <summary>
        /// Charge transaction (from creator, with initial coins).
        /// </summary>
        Charge = 3,

        /// <summary>
        /// Failed charge transaction (from creator, but less coins than needed).
        /// </summary>
        TooSmallForCharge = 4,

        /// <summary>
        /// Other user sent coins before pot had been charged.
        /// </summary>
        BeforeCharge = 5,
    }
}
