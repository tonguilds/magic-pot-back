namespace MagicPot.Backend.Services.Api
{
    public class BackupOptions
    {
        public TimeSpan Interval { get; set; } = TimeSpan.FromHours(4);

        public string FileName { get; set; } = "magicpot_{0:yyyyMMdd_HHmm}.db";

        public string? Location { get; set; }
    }
}
