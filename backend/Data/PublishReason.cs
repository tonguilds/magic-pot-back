namespace MagicPot.Backend.Data
{
    public enum PublishReason
    {
        /// <summary>
        /// Unused value.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Pot have recieved prize and now visible to users.
        /// </summary>
        PotCharged = 1,

        /// <summary>
        /// Pot have recieved first bet, and timer has started.
        /// </summary>
        PotActivated = 2,

        /// <summary>
        /// Pot has been stolen.
        /// </summary>
        PotStolen = 3,
    }
}
