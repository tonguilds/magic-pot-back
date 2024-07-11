namespace MagicPot.Backend.Utils
{
    using TonLibDotNet.Utils;

    public static class AddressConverter
    {
        public static string ToUser(string address)
        {
            if (!AddressValidator.TryParseAddress(address, out var workchainId, out var accountId, out var bounceable2, out var testnetOnly, out var urlSafe))
            {
                throw new ArgumentException("Invalid address", nameof(address));
            }

            return AddressValidator.MakeAddress(workchainId, accountId, false, !Program.InMainnet, urlSafe);
        }

        public static string ToContract(string address)
        {
            if (!AddressValidator.TryParseAddress(address, out var workchainId, out var accountId, out var bounceable2, out var testnetOnly, out var urlSafe))
            {
                throw new ArgumentException("Invalid address", nameof(address));
            }

            return AddressValidator.MakeAddress(workchainId, accountId, true, !Program.InMainnet, urlSafe);
        }

        public static string ToRaw(string address)
        {
            if (!AddressValidator.TryParseAddress(address, out var workchainId, out var accountId, out var bounceable, out var testnetOnly, out var urlSafe))
            {
                throw new ArgumentException("Invalid address", nameof(address));
            }

            return (workchainId == 255 ? "-1" : workchainId.ToString("X")) + ":" + Convert.ToHexString(accountId);
        }
    }
}
