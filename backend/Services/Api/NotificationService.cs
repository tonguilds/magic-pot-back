namespace MagicPot.Backend.Services.Api
{
    public class NotificationService(ILogger<NotificationService> logger, IServiceProvider serviceProvider)
        : NotificationServiceBase(logger, serviceProvider, StartupApi.RegisteredTasks)
    {
        // Nothing
    }
}
