namespace MagicPot.Backend.Services
{
    using MagicPot.Backend.Data;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using SQLite;

    public class MemoryDbProvider : DbProvider
    {
        public MemoryDbProvider(IOptions<BackendOptions> options, ILogger<DbProvider> logger)
            : base(options, logger)
        {
            // Nothing
        }

        protected override SQLiteConnection GetMainDb(BackendOptions options)
        {
            return new SQLiteConnection(":memory:");
        }
    }
}
