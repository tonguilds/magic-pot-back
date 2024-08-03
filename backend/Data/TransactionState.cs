namespace MagicPot.Backend.Data
{
    public enum TransactionState
    {
        /// <summary>
        /// Transaction has not been processed yet.
        /// </summary>
        Unprocessed = 0,

        /// <summary>
        /// Invalid transaction: in_msg was empty.
        /// </summary>
        InvalidNoInMsg = 10,

        /// <summary>
        /// Invalid transaction: in_msg.sender was empty.
        /// </summary>
        InvalidNoSender = 11,

        /// <summary>
        /// Transaction is a fake/unknown jetton tranfer (notification arrived not from our jetton wallet).
        /// </summary>
        InvalidUnknownJetton = 12,

        /// <summary>
        /// Transaction is an invalid jetton tranfer (forward_payload is invalid/wrong).
        /// </summary>
        InvalidBadPayload = 13,

        /// <summary>
        /// Transaction is of unknown type, skipped.
        /// </summary>
        SkippedUnknown = 20,

        /// <summary>
        /// Transaction is a successful Pot Charge.
        /// </summary>
        ChargeOk = 30,

        /// <summary>
        /// Transaction is an unsuccessful Pot Charge - only owner can charge pot.
        /// </summary>
        ChargeNotFromOwner = 31,

        /// <summary>
        /// Transaction is an unsuccessful Pot Charge - pot is already charged.
        /// </summary>
        ChargeAlreadyDone = 32,

        /// <summary>
        /// Transaction is an unsuccessful Pot Charge - amount is less than required.
        /// </summary>
        ChargeTooSmall = 33,

        /// <summary>
        /// Succefful pot bet.
        /// </summary>
        BetOk = 50,

        /// <summary>
        /// Unuccefful pot bet - pot is not charged yet.
        /// </summary>
        BetBeforeCharge = 51,

        /// <summary>
        /// Unsuccefful pot bet - pot is already stolen.
        /// </summary>
        BetAfterStolen = 52,

        /// <summary>
        /// Unsuccefful pot bet - amount is less than required.
        /// </summary>
        BetTooSmall = 53,

        /// <summary>
        /// Transaction is unknown. Ignored.
        /// </summary>
        UnknownIgnored = 255,
    }
}
