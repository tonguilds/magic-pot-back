namespace MagicPot.Backend.Services
{
    using System.IO;
    using RecurrentTasks;

    public abstract class NotificationServiceBase : INotificationService, IDisposable
    {
        private readonly ILogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly Dictionary<string, Type> knownTasks;
        private readonly FileSystemWatcher watcher;

        private bool disposed;

        protected NotificationServiceBase(ILogger logger, IServiceProvider serviceProvider, IReadOnlyList<Type> knownTasks)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.knownTasks = knownTasks.ToDictionary(x => GetFileName(x.GetGenericArguments()[0]), x => x, StringComparer.OrdinalIgnoreCase);

            this.watcher = new FileSystemWatcher(Environment.CurrentDirectory)
            {
                NotifyFilter = NotifyFilters.LastWrite,
                EnableRaisingEvents = true,
            };

            watcher.Changed += Watcher_Changed;

            logger.LogDebug("Watching in {Directory} for {Count} items", watcher.Path, knownTasks.Count);
        }

        public void TryRun<TRunnable>()
            where TRunnable : IRunnable
        {
            if (!RunKnown<TRunnable>())
            {
                NotifyUnknown<TRunnable>();
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected static string GetFileName(Type type)
        {
            return ".notify_" + type.Name;
        }

        protected static string GetFileName<TRunnable>()
            where TRunnable : IRunnable
        {
            return GetFileName(typeof(TRunnable));
        }

        protected void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Name) && knownTasks.TryGetValue(e.Name, out var type))
            {
                logger.LogDebug("Notified about {Type}", e.Name);
                ((ITask)serviceProvider.GetRequiredService(type)).TryRunImmediately();
            }
        }

        protected bool RunKnown<TRunnable>()
            where TRunnable : IRunnable
        {
            if (!knownTasks.Any(x => x.Value.IsAssignableTo(typeof(ITask<TRunnable>))))
            {
                return false;
            }

            var task = serviceProvider.GetRequiredService<ITask<TRunnable>>();
            task.TryRunImmediately();
            return true;
        }

        protected void NotifyUnknown<TRunnable>()
            where TRunnable : IRunnable
        {
            logger.LogDebug("Notifying about {Type}", typeof(TRunnable).Name);
            File.CreateText(GetFileName<TRunnable>()).Close();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    watcher?.Dispose();
                }

                disposed = true;
            }
        }
    }
}
