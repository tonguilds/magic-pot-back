namespace MagicPot.Backend.Data
{
    using SQLite;

    public class PublishQueueItem
    {
        [PrimaryKey]
        [AutoIncrement]
        public int Id { get; set; }

        [NotNull]
        public long PotId { get; set; }

        [NotNull]
        public PublishReason Reason { get; set; }

        public static PublishQueueItem Create(long potId, PublishReason reason)
        {
            return new PublishQueueItem { PotId = potId, Reason = reason };
        }
    }
}
