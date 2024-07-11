namespace MagicPot.Backend
{
    using System.Numerics;
    using MagicPot.Backend.Services.Api;
    using RecurrentTasks;
    using TonLibDotNet;
    using TonLibDotNet.Cells;
    using TonLibDotNet.Recipes;

    public static class Extensions
    {
        public static void ReloadCachedData(this HttpContext httpContext)
        {
            ReloadCachedData(httpContext.RequestServices);
        }

        public static void ReloadCachedData(this IServiceProvider serviceProvider)
        {
            serviceProvider.GetRequiredService<ITask<CachedData>>().TryRunImmediately();
        }

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
    }
}
