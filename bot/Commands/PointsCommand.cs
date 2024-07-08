namespace MagicPot.Bot.Commands
{
    using NetTelegramBot.Framework;
    using NetTelegramBotApi.Requests;
    using NetTelegramBotApi.Types;

    public class PointsCommand : ICommandHandler
    {
        public Task ExecuteAsync(ICommand command, User user, Chat chat, BotBase bot, Update update, IServiceProvider serviceProvider)
        {
            var text = string.Format(Messages.PointsCommandText, user.FirstName ?? user.Username, "-unknown-");

            var msg = new SendMessage(chat.Id, text)
            {
                ParseMode = SendMessage.ParseModeEnum.None,
                ReplyToMessageId = update.Message.MessageId,
            };

            return bot.SendAsync(msg);
        }
    }
}
