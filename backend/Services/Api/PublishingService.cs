namespace MagicPot.Backend.Services.Api
{
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Text;
    using MagicPot.Backend.Data;
    using Microsoft.Extensions.Options;
    using RecurrentTasks;

    public class PublishingService(ILogger<PublishingService> logger, IDbProvider dbProvider, IOptions<BackendOptions> backendOptions, HttpClient httpClient)
        : IRunnable
    {
        public static readonly TimeSpan DefaultInterval = TimeSpan.FromMinutes(5);

        private static readonly TimeSpan HaveMoreDataInterval = TimeSpan.FromSeconds(2);
        private static readonly CultureInfo DefaultCulture = new("ru-RU");

        private static readonly int MaxBatch = 100;
        private static readonly char[] MarkdownToEscape = ['\\', '_', '*', '[', ']', '(', ')', '~', '`', '>', '#', '+', '-', '=', '|', '{', '}', '.', '!'];

        private readonly BackendOptions options = backendOptions.Value;

        [return: NotNullIfNotNull(nameof(source))]
        public static string? MarkdownEscape(string? source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return source;
            }

            if (source.IndexOfAny(MarkdownToEscape) == -1)
            {
                return source;
            }

            var sb = new StringBuilder(source.Length + 50);
            foreach (var c in source)
            {
                if (MarkdownToEscape.Contains(c))
                {
                    sb.Append('\\');
                }

                sb.Append(c);
            }

            return sb.ToString();
        }

        public async Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            currentTask.Options.Interval = DefaultInterval;

            if (string.IsNullOrWhiteSpace(options.TelegramPublishingChatId))
            {
                logger.LogWarning("Telegram publishing chat_id not set.");
                return;
            }

            var count = 0;
            while (count < MaxBatch)
            {
                var item = dbProvider.MainDb.Table<PublishQueueItem>().FirstOrDefault();
                if (item == null)
                {
                    logger.LogTrace("Queue is empty");
                    return;
                }

                await Publish(item);

                dbProvider.MainDb.Delete(item);
                count++;
            }

            if (count >= MaxBatch)
            {
                currentTask.Options.Interval = HaveMoreDataInterval;
            }
        }

        public async Task Publish(PublishQueueItem item)
        {
            var pot = dbProvider.MainDb.Find<Pot>(item.PotId);
            if (pot == null)
            {
                logger.LogWarning("Pot not found: {Id}", item.PotId);
                return;
            }

            var jetton = dbProvider.MainDb.Get<Jetton>(x => x.Address == pot.JettonMaster);

            var text = item.Reason switch
            {
                PublishReason.PotCharged => GeneratePotCreatedMessageText(pot, jetton),
                PublishReason.PotActivated => GeneratePotActivatedMessageText(pot, jetton),
                PublishReason.PotStolen => GeneratePotStolenMessageText(pot, jetton),
                _ => string.Empty,
            };

            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            var (data, path) = CreatePotMessage(text, pot, options);

            var url = $"https://api.telegram.org/bot{options.TelegramBotToken}/{path}";
            using var resp = await httpClient.PostAsJsonAsync(url, data);
            if (!resp.IsSuccessStatusCode)
            {
                var respText = await resp.Content.ReadAsStringAsync();
                logger.LogDebug("Response: {Text}", respText);

                // and throw it
                resp.EnsureSuccessStatusCode();
            }

            logger.LogInformation("Published info about {Event} of pot {Key}", item.Reason, pot.Key);
        }

        protected static string CreateLinkToPot(string key)
        {
            return "https://t.me/magic_pot_bot/magicpot?startapp=" + key;
        }

        protected static string GeneratePotCreatedMessageText(Pot pot, Jetton jetton)
        {
            return @$"{Emoji.GlowingStar} New pot *{MarkdownEscape(pot.Name)}* created\!

Size: *{pot.InitialSize.ToString("N0", DefaultCulture)} {MarkdownEscape(jetton.Symbol)}*";
        }

        protected static string GeneratePotActivatedMessageText(Pot pot, Jetton jetton)
        {
            return @$"{Emoji.HighVoltage} First bet in *{MarkdownEscape(pot.Name)}*\!

{Emoji.HourglassNotDone} Countdown started\! Make your bet to steal this pot with *{pot.TotalSize.ToString("N0", DefaultCulture)} {MarkdownEscape(jetton.Symbol)}*";
        }

        protected static string GeneratePotStolenMessageText(Pot pot, Jetton jetton)
        {
            return @$"{Emoji.SlotMachine} *{MarkdownEscape(pot.Name)}* has been stolen\!

{Emoji.MoneyBag} *{pot.TotalSize.ToString("N0", DefaultCulture)} {MarkdownEscape(jetton.Symbol)}* will be delivered soon to winner's wallets\.";
        }

        protected static (object Data, string Path) CreatePotMessage(string text, Pot pot, BackendOptions options)
        {
            var replyMarkup = new
            {
                inline_keyboard = new[]
                {
                    new[]
                    {
                        new { text = "Steal the pot", url = CreateLinkToPot(pot.Key) },
                    },
                },
            };

            if (string.IsNullOrWhiteSpace(pot.CoverImage))
            {
                var data = new
                {
                    chat_id = options.TelegramPublishingChatId,
                    message_thread_id = options.TelegramPublishingThreadId,
                    text = text,
                    parse_mode = "MarkdownV2",
                    link_preview_options = new { is_disabled = true },
                    reply_markup = replyMarkup,
                };

                return (data, "sendMessage");
            }
            else if (!pot.CoverIsAnimated)
            {
                var data = new
                {
                    chat_id = options.TelegramPublishingChatId,
                    message_thread_id = options.TelegramPublishingThreadId,
                    photo = pot.CoverImage,
                    caption = text,
                    parse_mode = "MarkdownV2",
                    reply_markup = replyMarkup,
                };

                return (data, "sendPhoto");
            }
            else
            {
                var data = new
                {
                    chat_id = options.TelegramPublishingChatId,
                    message_thread_id = options.TelegramPublishingThreadId,
                    animation = pot.CoverImage,
                    caption = text,
                    parse_mode = "MarkdownV2",
                    reply_markup = replyMarkup,
                };

                return (data, "sendAnimation");
            }
        }
    }
}
