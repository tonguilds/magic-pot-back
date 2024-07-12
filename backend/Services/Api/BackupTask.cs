namespace MagicPot.Backend.Services.Api
{
    using System.Globalization;
    using MagicPot.Backend.Data;
    using Microsoft.Extensions.Options;
    using RecurrentTasks;

    public class BackupTask(ILogger<BackupTask> logger, IDbProvider dbProvider, IOptions<BackupOptions> options) : IRunnable
    {
        private readonly BackupOptions options = options.Value;

        public Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            currentTask.Options.Interval = options.Interval;

            var loc = options.Location;
            if (string.IsNullOrWhiteSpace(loc))
            {
                logger.LogWarning("Backup location is empty, skipped.");
                return Task.CompletedTask;
            }

            var dir = new DirectoryInfo(loc);
            if (!dir.Exists)
            {
                dir.Create();
                logger.LogDebug("Backup location created: {Path}", dir.FullName);
            }

            var fileName = string.Format(CultureInfo.InvariantCulture, options.FileName, DateTimeOffset.UtcNow);

            var file = Path.Combine(dir.FullName, fileName);

            dbProvider.MainDb.Backup(file);

            logger.LogInformation("Backup created: {File}", file);

            return Task.CompletedTask;
        }
    }
}
