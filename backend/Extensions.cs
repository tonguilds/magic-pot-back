namespace MagicPot.Backend
{
    using System.Numerics;
    using MagicPot.Backend.Services;
    using RecurrentTasks;
    using TonLibDotNet;
    using TonLibDotNet.Cells;
    using TonLibDotNet.Recipes;
    using TonLibDotNet.Utils;

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

        public static string MakeRaw(this AddressUtils addressUtils, string address)
        {
            if (!AddressValidator.TryParseAddress(address, out var workchainId, out var accountId, out var bounceable, out var testnetOnly, out var urlSafe))
            {
                throw new ArgumentException("Invalid address", nameof(address));
            }

            return (workchainId == 255 ? "-1" : workchainId.ToString("X")) + ":" + Convert.ToHexString(accountId);
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
