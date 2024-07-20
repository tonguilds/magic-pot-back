namespace MagicPot.Backend.Services
{
    using RecurrentTasks;

    public interface INotificationService
    {
        void TryRun<TRunnable>()
            where TRunnable : IRunnable;
    }
}
