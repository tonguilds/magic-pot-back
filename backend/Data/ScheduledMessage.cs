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

        public static ScheduledMessage Create(long potId, ScheduledMessageType type, long? userId)
        {
            return new()
            {
                PotId = potId,
                Type = type,
                UserId = userId,
            };
        }
    }
}
