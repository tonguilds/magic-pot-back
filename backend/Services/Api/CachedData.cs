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

        public List<Jetton> AllJettons { get; private set; } = [];

        public List<Pot> ActivePots { get; private set; } = [];

        public HashSet<string> AllPotKeys { get; private set; } = [];

        public long TotalUsers { get; private set; } = 0;

        public Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            Options = scopeServiceProvider.GetRequiredService<IOptionsSnapshot<BackendOptions>>().Value;

            if (!string.IsNullOrWhiteSpace(Options.TelegramBotToken))
            {
                var tokenBytes = System.Text.Encoding.ASCII.GetBytes(Options.TelegramBotToken);
                var keyBytes = System.Text.Encoding.ASCII.GetBytes("WebAppData");
                TelegramBotTokenWebappdataHash = HMACSHA256.HashData(keyBytes, tokenBytes);
            }

            var db = scopeServiceProvider.GetRequiredService<IDbProvider>().MainDb;

            LastKnownSeqno = db.Find<Settings>(Settings.KeyLastSeqno)?.LongValue ?? default;
            ActivePots = db.Table<Pot>().Where(x => x.Charged != null && x.Paid == null).ToList();
            AllPotKeys = db.Table<Pot>().Select(x => x.Key).ToHashSet(StringComparer.Ordinal);
            AllJettons = db.Table<Jetton>().ToList();
            TotalUsers = db.Table<User>().Count();

            var logger = scopeServiceProvider.GetRequiredService<ILogger<CachedData>>();
            logger.LogDebug(
                "Reloaded: KnownJettons={Count1}, ActivePots={Count2}, TotalPotKeys={Count3}, TotalUsers={Total}",
                AllJettons.Count,
                ActivePots.Count,
                AllPotKeys.Count,
                TotalUsers);

            return Task.CompletedTask;
        }
    }
}
