namespace MagicPot.Backend.Services.Indexer
{
    using System;
    using Microsoft.Extensions.Logging;

    public class NotificationService(ILogger<NotificationService> logger, IServiceProvider serviceProvider)
        : NotificationServiceBase(logger, serviceProvider, StartupIndexer.RegisteredTasks)
    {
        // Nothing
    }
}
