namespace MagicPot.Backend.Services.Api
{
    using System.Globalization;
    using MagicPot.Backend.Data;
    using MagicPot.Backend.Utils;
    using Microsoft.Extensions.Options;
    using RecurrentTasks;

    public class ScheduledMessageSender(ILogger<ScheduledMessageSender> logger, IDbProvider dbProvider, IOptions<BackendOptions> backendOptions, HttpClient httpClient)
        : IRunnable
    {
        public static readonly TimeSpan DefaultInterval = TimeSpan.FromMinutes(5);

        private static readonly TimeSpan HaveMoreDataInterval = TimeSpan.FromSeconds(2);
        private static readonly CultureInfo DefaultCulture = new("ru-RU");

        private static readonly int MaxBatch = 100;

        private readonly BackendOptions options = backendOptions.Value;

        public async Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            currentTask.Options.Interval = DefaultInterval;

            var count = 0;
            while (count < MaxBatch)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                var item = dbProvider.MainDb.Table<ScheduledMessage>().FirstOrDefault();
                if (item == null)
                {
                    logger.LogTrace("Queue is empty");
                    break;
                }

                try
                {
                    await Send(item);
                }
                finally
                {
                    dbProvider.MainDb.Delete(item);
                }

                count++;
            }

            if (count >= MaxBatch)
            {
                currentTask.Options.Interval = HaveMoreDataInterval;
            }
        }

        public async Task Send(ScheduledMessage msg)
        {
            var pot = dbProvider.MainDb.Find<Pot>(msg.PotId);
            if (pot == null)
            {
                logger.LogWarning("Pot not found: {Id}", msg.PotId);
                return;
            }

            if (msg.UserId != null)
            {
                var user = dbProvider.MainDb.Find<User>(msg.UserId.Value);
                if (user != null && !user.AllowsWriteToPM)
                {
                    logger.LogWarning("User {Id} does not allow PM, notification of {Type} for {Pot} skipped", user.Id, msg.Type, pot.Key);
                    return;
                }
            }
            else if (options.TelegramPublishingChatId == null || options.TelegramPublishingChatId == 0)
            {
                return;
            }
            else
            {
                msg.UserId = options.TelegramPublishingChatId;
            }

            var (data, path) = msg.Type switch
            {
                ScheduledMessageType.ReferralRichMessage => CreatePotRichMessage(pot, msg.UserId.Value, msg.Address),
                ScheduledMessageType.PotStarted => CreatePotStartedMessage(pot, pot.OwnerUserId),
                ScheduledMessageType.PotTransactionAccepted => CreatePotTransactionAcceptedMessage(pot, msg.UserId.Value),
                ScheduledMessageType.PotTransactionDeclined => CreatePotTransactionDeclinedMessage(pot, msg.UserId.Value),
                _ => (new object(), string.Empty),
            };

            if (string.IsNullOrEmpty(path))
            {
                logger.LogWarning("Message type {Type}, ignored", msg.Type);
                return;
            }

            var url = $"https://api.telegram.org/bot{options.TelegramBotToken}/{path}";
            using var resp = await httpClient.PostAsJsonAsync(url, data);
            if (!resp.IsSuccessStatusCode)
            {
                var respText = await resp.Content.ReadAsStringAsync();
                logger.LogDebug("Response: {Text}", respText);

                // and throw it
                resp.EnsureSuccessStatusCode();
            }

            logger.LogInformation("Published info about {Event} of pot {Key} to {Id}", msg.Type, pot.Key, msg.UserId);
        }

        protected static object CreateReplyMarkup(Pot pot, string? refAddress = null, string text = "Steal the pot")
        {
            var url = string.IsNullOrWhiteSpace(refAddress)
                ? "https://t.me/magic_pot_bot/magicpot?startapp=pull" + pot.Key
                : "https://t.me/magic_pot_bot/magicpot?startapp=ref" + pot.Key + "__" + refAddress;

            return new
            {
                inline_keyboard = new[]
                {
                    new[]
                    {
                        new { text = text, url = url },
                    },
                },
            };
        }

        protected (object Data, string Path) CreatePotRichMessage(Pot pot, long targetUser, string? refAddress)
        {
            var creator = dbProvider.MainDb.Get<User>(pot.OwnerUserId);
            var jetton = dbProvider.MainDb.Get<Jetton>(x => x.Address == pot.JettonMaster);

            var jettonLink = Program.InMainnet
                ? "https://tonscan.org/address/" + jetton.Address
                : "https://testnet.tonscan.org/address/" + jetton.Address;

            var text = $@"*{MarkdownEncoder.Escape(pot.Name)}*
• ᴄʀᴇᴀᴛᴏʀ: [@{MarkdownEncoder.Escape(creator.Username ?? creator.FirstName)}](tg://user?id={pot.OwnerUserId})
• ɪɴɪᴛɪᴀʟ ᴘᴏᴏʟ: {MarkdownEncoder.Escape(pot.InitialSize.ToString("N0", DefaultCulture))} [{MarkdownEncoder.Escape(jetton.Symbol)}]({jettonLink})
• ᴄᴏᴜɴᴛᴅᴏᴡɴ: {Math.Floor(pot.Countdown.TotalHours)}`h` {pot.Countdown.Minutes:00}`m`
• ʙᴇᴛ ᴛʏᴘᴇ: {(pot.TxSizeIncrease == 0 ? "fixed" : "increasing " + pot.TxSizeIncrease + "%")}

*Who gets the prize:*
";

            if (pot.CreatorPercent > 0)
            {
                text += $"• ᴄʀᴇᴀᴛᴏʀ: {pot.CreatorPercent}%" + Environment.NewLine;
            }

            if (pot.LastTxPercent > 0)
            {
                text += $"• {pot.LastTxCount} ʟᴀꜱᴛ ᴘʟᴀʏᴇʀꜱ: {pot.LastTxPercent}%" + Environment.NewLine;
            }

            if (pot.ReferrersPercent > 0)
            {
                text += $"• ʀᴇꜰᴇʀʀᴇʀꜱ: {pot.ReferrersPercent}%" + Environment.NewLine;
            }

            if (pot.RandomTxPercent > 0)
            {
                text += $"• {pot.RandomTxCount} ʀᴀɴᴅᴏᴍ ᴘʟᴀʏᴇʀꜱ: {pot.RandomTxPercent}%" + Environment.NewLine;
            }

            if (pot.BurnPercent > 0)
            {
                text += $"• ʙᴜʀɴ: {pot.BurnPercent}%" + Environment.NewLine;
            }

            if (string.IsNullOrEmpty(refAddress))
            {
                refAddress = pot.OwnerUserAddress;
            }

            var replyMarkup = CreateReplyMarkup(pot, refAddress, "Open pot");

            if (string.IsNullOrWhiteSpace(pot.CoverImage))
            {
                var data = new
                {
                    chat_id = targetUser,
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
                    chat_id = targetUser,
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
                    chat_id = targetUser,
                    animation = pot.CoverImage,
                    caption = text,
                    parse_mode = "MarkdownV2",
                    reply_markup = replyMarkup,
                };

                return (data, "sendAnimation");
            }
        }

        protected (object Data, string Path) CreatePotStartedMessage(Pot pot, long targetUser)
        {
            var text = $@"Hey\! Someone has just sent the first transaction into your pot *{MarkdownEncoder.Escape(pot.Name)}*\. The countdown has begun, good luck with the game\!";

            var data = new
            {
                chat_id = targetUser,
                text = text,
                parse_mode = "MarkdownV2",
                reply_markup = CreateReplyMarkup(pot),
            };

            return (data, "sendMessage");
        }

        protected (object Data, string Path) CreatePotTransactionAcceptedMessage(Pot pot, long targetUser)
        {
            var text = $@"Hey\! Your transaction reached the pot *{MarkdownEncoder.Escape(pot.Name)}*\. You are now in the game, good luck\!";

            var data = new
            {
                chat_id = targetUser,
                text = text,
                parse_mode = "MarkdownV2",
                reply_markup = CreateReplyMarkup(pot),
            };

            return (data, "sendMessage");
        }

        protected (object Data, string Path) CreatePotTransactionDeclinedMessage(Pot pot, long targetUser)
        {
            var text = $@"Hey\! Your transaction has reached the pot *{MarkdownEncoder.Escape(pot.Name)}* but didn't participate because someone sent the same amount slightly before you\. You can reclaim your tokens on the pot page using the ""Return failed bid"" button\.";

            var data = new
            {
                chat_id = targetUser,
                text = text,
                parse_mode = "MarkdownV2",
                reply_markup = CreateReplyMarkup(pot),
            };

            return (data, "sendMessage");
        }
    }
}
