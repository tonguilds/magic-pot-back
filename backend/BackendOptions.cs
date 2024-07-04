namespace MagicPot.Backend
{
    public class BackendOptions
    {
        public const string TelegramInitDataHeaderName = "X_InitData";

        public string DatabaseFile { get; set; } = "./backend.db";

        public string CacheDirectory { get; set; } = "./cache";

        public TimeSpan CacheReloadInterval { get; set; } = TimeSpan.FromMinutes(15);

        public PathString SearchCacheUpdatePath { get; set; } = "/update-search-cache";

        public PathString HealthReportPath { get; set; } = "/update-indexer-health-data";

        public TimeSpan IndexerSubprocessRestartInterval { get; set; } = TimeSpan.FromHours(2);

        public string TelegramBotTokenHash { get; set; } = string.Empty;

        public TimeSpan TelegramInitDataValidity { get; set; } = TimeSpan.FromHours(1);

        public Dictionary<string, string> WellKnownJettons { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
