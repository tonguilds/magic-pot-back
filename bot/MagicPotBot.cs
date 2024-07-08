namespace MagicPot.Bot
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using MagicPot.Bot.Commands;
    using Microsoft.Extensions.Logging;
    using NetTelegramBot.Framework;
    using NetTelegramBotApi.Types;

    public class MagicPotBot : BotBase
    {
        public MagicPotBot(ILogger<MagicPotBot> logger, ICommandParser commandParser, IHttpClientFactory httpClientFactory)
            : base(logger, commandParser, httpClientFactory)
        {
            RegisteredCommandHandlers.Add("start", typeof(StartCommand));
            RegisteredCommandHandlers.Add("points", typeof(PointsCommand));
        }

        protected override Task OnUnhandledMessageAsync(Message message, Update update, IServiceProvider serviceProvider)
        {
            return Task.CompletedTask;
        }

        protected override Task OnUnhandledCallbackQueryAsync(CallbackQuery callbackQuery, Update update, IServiceProvider serviceProvider)
        {
            return Task.CompletedTask;
        }

        protected override Task OnUnhandledChannelPostAsync(Message channelPost, Update update, IServiceProvider serviceProvider)
        {
            return Task.CompletedTask;
        }

        protected override Task OnUnknownCommandAsync(ICommand command, User user, Chat chat, Update update, IServiceProvider serviceProvider)
        {
            return Task.CompletedTask;
        }
    }
}
