namespace MagicPot.Backend.Models
{
    using MagicPot.Backend.Data;

    public class PotInfo
    {
        public string Key { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public long CreatorId { get; set; }

        public string? CreatorName { get; set; } = string.Empty;

        public string? CreatorUsername { get; set; } = string.Empty;

        public string? CoverImage { get; set; }

        public decimal InitialSize { get; set; }

        public decimal TotalSize { get; set; }

        public decimal NextTransactionSize { get; set; }

        public string JettonSymbol { get; set; } = string.Empty;

        public string? JettonImage { get; set; }

        public bool IsWaitingForPrizeTransacton { get; set; }

        public bool IsStarted { get; set; }

        public bool IsEnded { get; set; }

        public DateTimeOffset? EndsAt { get; set; }

        public PotRules Rules { get; set; } = new();

        public List<PotParticipant> LastParticipants { get; set; } = [];

        public static PotInfo Create(Pot pot, Jetton jetton, User user, IList<PotTransaction> transactions, IList<User> knownUsers)
        {
            return new PotInfo
            {
                Key = pot.Key,
                Name = pot.Name,
                Address = pot.Address,
                CreatorId = user.Id,
                CreatorName = user.FirstName,
                CreatorUsername = user.Username,
                CoverImage = pot.CoverImage,
                InitialSize = pot.InitialSize,
                TotalSize = pot.TotalSize,
                NextTransactionSize = pot.TxSizeNext,
                JettonSymbol = jetton.Symbol,
                JettonImage = jetton.Image,
                IsWaitingForPrizeTransacton = !pot.Charged.HasValue,
                IsStarted = pot.FirstTx.HasValue,
                IsEnded = pot.Stolen.HasValue,
                EndsAt = pot.Stolen ?? pot.LastTx?.Add(pot.Countdown),
                Rules = new()
                {
                    Countdown = (int)pot.Countdown.TotalMinutes,
                    TransactionIncreasingPercent = pot.TxSizeIncrease,
                    CreatorPercent = pot.CreatorPercent,
                    LastTransactionsPercent = pot.LastTxPercent,
                    LastTransactionsCount = pot.LastTxCount,
                    RandomTransactionsPercent = pot.RandomTxPercent,
                    RandomTransactionsCount = pot.RandomTxCount,
                    ReferrersPercent = pot.ReferrersPercent,
                    BurnPercent = pot.BurnPercent,
                },
                LastParticipants = transactions
                    .Select(x => new PotParticipant
                    {
                        TxTime = x.Notified,
                        Address = x.Sender ?? string.Empty,
                        UserId = x.UserId ?? 0,
                        Name = knownUsers.FirstOrDefault(z => z.Id == x.UserId)?.FirstName,
                        Username = knownUsers.FirstOrDefault(z => z.Id == x.UserId)?.Username,
                    })
                    .ToList(),
            };
        }

        public class PotRules
        {
            /// <summary>
            /// Countdown (in minutes).
            /// </summary>
            public int Countdown { get; set; }

            public uint TransactionIncreasingPercent { get; set; }

            public uint CreatorPercent { get; set; }

            public uint LastTransactionsPercent { get; set; }

            public uint LastTransactionsCount { get; set; }

            public uint RandomTransactionsPercent { get; set; }

            public uint RandomTransactionsCount { get; set; }

            public uint ReferrersPercent { get; set; }

            public uint BurnPercent { get; set; }
        }

        public class PotParticipant
        {
            public DateTimeOffset TxTime { get; set; }

            public string Address { get; set; } = string.Empty;

            public long UserId { get; set; }

            public string? Name { get; set; }

            public string? Username { get; set; }
        }
    }
}
