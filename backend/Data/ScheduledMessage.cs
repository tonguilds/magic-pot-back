namespace MagicPot.Backend.Data
{
    using SQLite;

    public class ScheduledMessage
    {
        [PrimaryKey]
        [AutoIncrement]
        public int Id { get; set; }

        [NotNull]
        public long PotId { get; set; }

        [NotNull]
        public ScheduledMessageType Type { get; set; }

        public long? UserId { get; set; }

        public string? Address { get; set; }

        public string? TransactionHash { get; set; }

        public static ScheduledMessage Create(long potId, ScheduledMessageType type, long? userId, string? address = null, string? transactionHash = null)
        {
            return new()
            {
                PotId = potId,
                Type = type,
                UserId = userId,
                Address = address,
                TransactionHash = transactionHash,
            };
        }
    }
}
