namespace MagicPot.Backend
{
    using SQLite;

    public class Settings
    {
        public const string KeyDbVersion = "DB_VERSION";
        public const string KeyNetType = "NET_TYPE";
        public const string KeyLastSeqno = "LAST_SEQNO";

        [Obsolete("For data layer only")]
        public Settings()
            : this("?")
        {
            // Nothing
        }

        public Settings(string id)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            Id = id;
        }

        public Settings(string id, int value)
            : this(id)
        {
            IntValue = value;
        }

        public Settings(string id, long value)
            : this(id)
        {
            LongValue = value;
        }

        public Settings(string id, string value)
            : this(id)
        {
            StringValue = value;
        }

        public Settings(string id, DateTimeOffset value)
            : this(id)
        {
            DateTimeOffsetValue = value;
        }

        public Settings(string id, bool value)
            : this(id)
        {
            BoolValue = value;
        }

        [PrimaryKey]
        public string Id { get; set; }

        public string? StringValue { get; set; }

        public int? IntValue { get; set; }

        public long? LongValue { get; set; }

        public bool? BoolValue { get; set; }

        public DateTimeOffset? DateTimeOffsetValue { get; set; }
    }
}
