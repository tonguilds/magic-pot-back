namespace MagicPot.Backend.Services
{
    using MagicPot.Backend.Data;
    using Microsoft.Extensions.Options;
    using RecurrentTasks;

    public class CachedData : IRunnable
    {
        public bool InMainnet { get; private set; }

        public BackendOptions Options { get; private set; } = new();

        public List<Pool> AllPools { get; private set; } = [];

        public Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            Options = scopeServiceProvider.GetRequiredService<IOptionsSnapshot<BackendOptions>>().Value;

            var db = scopeServiceProvider.GetRequiredService<IDbProvider>();

            InMainnet = db.MainDb.Find<Settings>(Settings.KeyNetType)?.BoolValue ?? default;
            AllPools = db.MainDb.Table<Pool>().ToList();

            return Task.CompletedTask;
        }
    }
}
