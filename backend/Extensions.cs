namespace MagicPot.Backend
{
    using System.Numerics;
    using TonLibDotNet;
    using TonLibDotNet.Cells;
    using TonLibDotNet.Recipes;

    public static class Extensions
    {
        public static Cell CreateTransferCell(this Tep74Jettons tep74Jettons, ulong queryId, BigInteger amount, string destination, string responseDestination, Cell? customPayload, decimal forwardTonAmount, Cell? forwardPayload)
        {
            return new CellBuilder()
                .StoreUInt(260734629u)
                .StoreULong(queryId)
                .StoreCoins(amount)
                .StoreAddressIntStd(destination)
                .StoreAddressIntStd(responseDestination)
                .StoreDict(customPayload)
                .StoreCoins(TonUtils.Coins.ToNano(forwardTonAmount))
                .StoreDict(forwardPayload)
                .Build();
        }

        public static CellBuilder StoreAddressIntStd2(this CellBuilder builder, string? address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                builder.StoreBit(false);
                builder.StoreBit(false);
                return builder;
            }

            return builder.StoreAddressIntStd(address);
        }
    }
}
