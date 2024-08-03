namespace MagicPot.Backend.Utils
{
    using MagicPot.Backend.Data;
    using TonLibDotNet;
    using TonLibDotNet.Cells;

    public static class PayloadEncoder
    {
        private const byte Version1 = 0xF2;
        private const byte Version1RandLength = 19;
        private const int OpCodePrize = 0x26b3e995;
        private const int OpCodeBet = 0x7a795387;

        public static Cell EncodePrize(long userId)
        {
            return new CellBuilder()
                .StoreByte(Version1)
                .StoreInt(Random.Shared.Next(), Version1RandLength)
                .StoreInt(OpCodePrize)
                .StoreLong(userId)
                .Build();
        }

        public static Cell EncodeBet(long userId, string? referrerAddress)
        {
            return new CellBuilder()
                .StoreByte(Version1)
                .StoreInt(Random.Shared.Next(), Version1RandLength)
                .StoreInt(OpCodeBet)
                .StoreLong(userId)
                .StoreAddressIntStd2(referrerAddress)
                .Build();
        }

        public static bool TryDecode(Cell cell, out TransactionOpcode opcode, out long userId, out string? referrerAddress)
        {
            opcode = TransactionOpcode.DefaultNone;
            userId = 0;
            referrerAddress = null;

            try
            {
                var slice = cell.BeginRead();

                if (!slice.TryCanLoad(8))
                {
                    return false;
                }

                var ver = slice.LoadByte();
                if (ver != Version1)
                {
                    return false;
                }

                slice.SkipBits(Version1RandLength);
                opcode = slice.LoadInt() switch
                {
                    OpCodePrize => TransactionOpcode.PrizeTransfer,
                    OpCodeBet => TransactionOpcode.Bet,
                    _ => TransactionOpcode.DefaultNone,
                };

                userId = slice.LoadLong();

                if (opcode == TransactionOpcode.Bet)
                {
                    referrerAddress = slice.TryLoadAddressIntStd();
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }
    }
}
