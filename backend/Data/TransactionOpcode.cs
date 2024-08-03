namespace MagicPot.Backend.Data
{
    public enum TransactionOpcode : byte
    {
        /// <summary>
        /// No opcode or unknown opcode.
        /// </summary>
        DefaultNone = 0,

        /// <summary>
        /// Owner transfers initial prize.
        /// </summary>
        PrizeTransfer = 10,

        /// <summary>
        /// Bet from user.
        /// </summary>
        Bet = 20,
    }
}
