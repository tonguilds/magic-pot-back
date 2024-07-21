namespace MagicPot.Backend
{
    public class BackendOptions
    {
        public const string TelegramInitDataHeaderName = "X-InitData";

        public string DatabaseFile { get; set; } = "./backend.db";

        public string CacheDirectory { get; set; } = "./cache";

        public TimeSpan CacheReloadInterval { get; set; } = TimeSpan.FromMinutes(15);

        public PathString HealthReportPath { get; set; } = "/update-indexer-health-data";

        public TimeSpan IndexerSubprocessRestartInterval { get; set; } = TimeSpan.FromHours(2);

        public TimeSpan TelegramInitDataValidity { get; set; } = TimeSpan.FromHours(1);

        public string TelegramBotToken { get; set; } = string.Empty;

        public TimeSpan JettonBalanceValidity { get; set; } = TimeSpan.FromMinutes(15);
    }
}
