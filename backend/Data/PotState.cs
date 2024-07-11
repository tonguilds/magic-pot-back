namespace MagicPot.Backend.Data
{
    public enum PotState
    {
        /// <summary>
        /// Pot created and waiting for initial coins to arrive.
        /// </summary>
        Created = 0,

        /// <summary>
        /// Pot is charged with initial coins and waiting for first user transaction.
        /// </summary>
        Charged = 10,

        /// <summary>
        /// Pot is active.
        /// </summary>
        Ticking = 20,

        /// <summary>
        /// Pot has winner(s) and waiting for prize requests.
        /// </summary>
        Stolen = 30,

        /// <summary>
        /// Pot has been completely paid and not visible anymore.
        /// </summary>
        Paid = 40,
    }
}
