namespace MagicPot.Backend
{
    public class BackendOptions
    {
        public string DatabaseFile { get; set; } = "./backend.db";

        public TimeSpan CacheReloadInterval { get; set; } = TimeSpan.FromMinutes(15);

        public string TelegramBotTokenHash { get; set; } = string.Empty;

        public TimeSpan TelegramInitDataValidity { get; set; } = TimeSpan.FromHours(1);
    }
}
