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

        public Dictionary<string, Jetton> AllJettons { get; private set; } = [];

        public HashSet<string> AllPotKeys { get; private set; } = [];

        public long TotalUsers { get; private set; } = 0;

        public Dictionary<string, Pot> ActivePots { get; private set; } = [];

        public Dictionary<long, User> ActivePotOwners { get; private set; } = [];

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
            AllPotKeys = db.Table<Pot>().Select(x => x.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
            AllJettons = db.Table<Jetton>().ToDictionary(x => x.Address);
            TotalUsers = db.Table<User>().Count();
            ActivePots = db.Table<Pot>().Where(x => x.Charged != null && x.Paid == null).ToDictionary(x => x.Key, StringComparer.OrdinalIgnoreCase);
            var ownerIds = ActivePots.Select(x => x.Value.OwnerUserId).Distinct().ToArray();
            ActivePotOwners = db.Table<User>().Where(x => ownerIds.Contains(x.Id)).ToDictionary(x => x.Id);

            var logger = scopeServiceProvider.GetRequiredService<ILogger<CachedData>>();
            logger.LogDebug(
                "Reloaded: KnownJettons={Count1}, ActivePots={Count2}, TotalPotKeys={Count3}, TotalUsers={Total}, ActivePotOwners={Count4}",
                AllJettons.Count,
                ActivePots.Count,
                AllPotKeys.Count,
                TotalUsers,
                ActivePotOwners.Count);

            return Task.CompletedTask;
        }
    }
}
