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

        public List<User> ActivePotUsers { get; private set; } = [];

        public Dictionary<long, List<PotTransaction>> ActivePotTransactions { get; private set; } = [];

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

            var txCount = 0;
            ActivePotTransactions.Clear();
            foreach (var id in ActivePots.Values.Select(x => x.Id))
            {
                var txList = db.Table<PotTransaction>()
                    .Where(x => x.PotId == id && x.State == PotTransactionState.Bet)
                    .OrderByDescending(x => x.Notified)
                    .Take(10)
                    .ToList();
                ActivePotTransactions[id] = txList;
                txCount += txList.Count;
            }

            var activePotUserIds = ActivePotTransactions.SelectMany(x => x.Value.Select(z => z.UserId)).Where(x => x != null).Distinct().ToList();
            ActivePotUsers = db.Table<User>().Where(x => activePotUserIds.Contains(x.Id)).ToList();

            var logger = scopeServiceProvider.GetRequiredService<ILogger<CachedData>>();
            logger.LogDebug(
                "Reloaded: KnownJettons={Count1}, ActivePots={Count2}, TotalPotKeys={Count3}, TotalUsers={Total}, ActivePotOwners={Count4}, ActivePotUsers={Count5}, LastTx={Count6}",
                AllJettons.Count,
                ActivePots.Count,
                AllPotKeys.Count,
                TotalUsers,
                ActivePotOwners.Count,
                ActivePotUsers.Count,
                txCount);

            return Task.CompletedTask;
        }
    }
}
