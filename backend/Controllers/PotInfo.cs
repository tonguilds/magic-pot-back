namespace MagicPot.Backend.Controllers
{
    using MagicPot.Backend.Data;

    public class PotInfo
    {
        public string Key { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? CreatorName { get; set; } = string.Empty;

        public string? CreatorUsername { get; set; } = string.Empty;

        public string? CoverImage { get; set; }

        public decimal InitialSize { get; set; }

        public decimal TotalSize { get; set; }

        public decimal NextTransactionSize { get; set; }

        public string JettonSymbol { get; set; } = string.Empty;

        public string? JettonImage { get; set; }

        public bool WaitingForPrizeTransacton { get; set; }

        public PotRules Rules { get; set; } = new();

        public static PotInfo Create(Pot pot, Jetton jetton, User user)
        {
            return new PotInfo
            {
                Key = pot.Key,
                Name = pot.Name,
                CreatorName = user.FirstName,
                CreatorUsername = user.Username,
                CoverImage = pot.CoverImage,
                InitialSize = pot.InitialSize,
                TotalSize = pot.TotalSize,
                NextTransactionSize = pot.TxSizeNext,
                JettonSymbol = jetton.Symbol,
                JettonImage = jetton.Image,
                WaitingForPrizeTransacton = !pot.Charged.HasValue,
                Rules = new()
                {
                    Countdown = (int)pot.Countdown.TotalMinutes,
                    TransactionIncreasingPercent = pot.TxSizeIncrease,
                    CreatorPercent = pot.CreatorPercent,
                    LastTransactionsPercent = pot.LastTxPercent,
                    LastTransactionsCount = pot.LastTxCount,
                    RandomTransactionsPercent = pot.RandomTxPercent,
                    RandomTransactionsCount = pot.RandomTxCount,
                    ReferralsPercent = pot.ReferralsPercent,
                    BurnPercent = pot.BurnPercent,
                },
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

            public uint ReferralsPercent { get; set; }

            public uint BurnPercent { get; set; }
        }
    }
}
