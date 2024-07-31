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
        /// Transaction is a manual jetton tranfer (forward_payload mismatch).
        /// </summary>
        ManualTransfer = 2,

        /// <summary>
        /// Transaction is fine, and waiting for additional classification.
        /// </summary>
        Processing = 10,

        /// <summary>
        /// Charge transaction (from creator, with initial coins).
        /// </summary>
        Charge = 30,

        /// <summary>
        /// Failed charge transaction (from creator, but less coins than needed).
        /// </summary>
        TooSmallForCharge = 31,

        /// <summary>
        /// Other user sent coins before pot had been charged.
        /// </summary>
        BeforeCharge = 32,

        /// <summary>
        /// Transaction is a valid bet.
        /// </summary>
        Bet = 40,

        /// <summary>
        /// Transaction value is less than required.
        /// </summary>
        TooSmallForBet = 41,

        /// <summary>
        /// Transaction arrived after the Pot had been stolen.
        /// </summary>
        AfterStolen = 51,
    }
}
