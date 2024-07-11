namespace MagicPot.Backend.Services.Api
{
    using System.Diagnostics;
    using Microsoft.Extensions.Options;
    using RecurrentTasks;

    public class IndexerControlTask(ILogger<IndexerControlTask> logger, IOptions<BackendOptions> options) : IRunnable, IDisposable
    {
        public static readonly TimeSpan Interval = TimeSpan.FromSeconds(15);

        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly TimeSpan indexerRestartInterval = options.Value.IndexerSubprocessRestartInterval;

        private Process? indexer;
        private DateTimeOffset indexerRestartTime = DateTimeOffset.MaxValue;
        private bool disposed;

        public Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            if (indexer != null)
            {
                indexer.Refresh();

                if (indexer.HasExited)
                {
                    logger.LogDebug("Indexer process HasExited=true, disposing...");
                    indexer.Close();
                    indexer = null;
                }
                else if (DateTimeOffset.UtcNow > indexerRestartTime)
                {
                    logger.LogDebug("Indexer process running too long, killing...");
                    indexer.Kill();
                }
            }

            indexer ??= StartIndexer();

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected Process StartIndexer()
        {
            var psi = new ProcessStartInfo
            {
                FileName = Environment.ProcessPath,
                WorkingDirectory = Environment.CurrentDirectory,
                ArgumentList =
                    {
                        Program.StartAsIndexerArg,
                    },
            };

            logger.LogDebug("WorkingDir: {Dir}", psi.WorkingDirectory);
            logger.LogDebug("Executable: {Exe}", psi.FileName);

            indexerRestartTime = indexerRestartInterval.Ticks > 0
                ? DateTimeOffset.UtcNow.Add(indexerRestartInterval)
                : DateTimeOffset.MaxValue;

            var p = Process.Start(psi)!;

            logger.LogInformation("Process started: PID={PID}", p.Id);

            return p;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                logger.LogDebug("I'm being disposed");

                if (disposing)
                {
                    if (indexer != null)
                    {
                        indexer.Kill();
                        indexer.WaitForExit();
                        logger.LogInformation("Process killed: PID={PID}", indexer.Id);
                    }
                }

                disposed = true;
            }
        }
    }
}
