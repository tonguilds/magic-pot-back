namespace MagicPot.Bot
{
    using RecurrentTasks;

    public class InitialConnectionTask(ILogger<InitialConnectionTask> logger, MagicPotBot bot) : IRunnable
    {
        public Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            // Nothing, just create bot
            logger.LogDebug("Bot is @{Username}", bot.Username);
            currentTask.Options.Interval = TimeSpan.Zero;
            return Task.CompletedTask;
        }
    }
}
