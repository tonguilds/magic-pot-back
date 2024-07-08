namespace MagicPot.Bot.Commands
{
    using NetTelegramBot.Framework;
    using NetTelegramBotApi.Requests;
    using NetTelegramBotApi.Types;

    public class StartCommand : ICommandHandler
    {
        public Task ExecuteAsync(ICommand command, User user, Chat chat, BotBase bot, Update update, IServiceProvider serviceProvider)
        {
            var text = string.Format(Messages.StartCommandText, user.FirstName ?? user.Username);

            var msg = new SendMessage(chat.Id, text)
            {
                ParseMode = SendMessage.ParseModeEnum.None,
                DisableWebPagePreview = true,
                ReplyMarkup = new InlineKeyboardMarkup
                {
                    InlineKeyboard = [
                        [new InlineKeyboardButton { Text = Messages.CreatePot, WebApp = new() { Url = "https://magic-pot-frontend.vercel.app/" } }],
                        [new InlineKeyboardButton { Text = Messages.Announcements, Url = "https://t.me/themagicpot" }],
                        [
                            new InlineKeyboardButton { Text = Messages.Chat, Url = "https://t.me/themagicpot_fam" },
                            new InlineKeyboardButton { Text = Messages.Faq, Url = "https://google.com" },
                        ],
                    ],
                },
            };

            return bot.SendAsync(msg);
        }
    }
}
