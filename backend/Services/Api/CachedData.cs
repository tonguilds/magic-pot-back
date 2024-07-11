namespace MagicPot.Backend.Services.Api
{
    using System.Security.Cryptography;
    using MagicPot.Backend.Data;
    using Microsoft.Extensions.Options;
    using RecurrentTasks;

    public class CachedData : IRunnable
    {
        public BackendOptions Options { get; private set; } = new();

        public byte[] TelegramBotTokenWebappdataHash { get; private set; } = [];

        public long LastKnownSeqno { get; private set; }

        public List<Jetton> KnownJettons { get; private set; } = [];

        public List<Pot> AllPools { get; private set; } = [];

        public Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            Options = scopeServiceProvider.GetRequiredService<IOptionsSnapshot<BackendOptions>>().Value;

            if (!string.IsNullOrWhiteSpace(Options.TelegramBotToken))
            {
                var tokenBytes = System.Text.Encoding.ASCII.GetBytes(Options.TelegramBotToken);
                var keyBytes = System.Text.Encoding.ASCII.GetBytes("WebAppData");
                TelegramBotTokenWebappdataHash = HMACSHA256.HashData(keyBytes, tokenBytes);
            }

            var db = scopeServiceProvider.GetRequiredService<IDbProvider>();

            LastKnownSeqno = db.MainDb.Find<Settings>(Settings.KeyLastSeqno)?.LongValue ?? default;
            AllPools = db.MainDb.Table<Pot>().ToList();
            KnownJettons = db.MainDb.Table<Jetton>().ToList();

            var logger = scopeServiceProvider.GetRequiredService<ILogger<CachedData>>();
            logger.LogDebug(
                "Reloaded: KnownJettons={Count}",
                KnownJettons.Count);

            return Task.CompletedTask;
        }
    }
}
