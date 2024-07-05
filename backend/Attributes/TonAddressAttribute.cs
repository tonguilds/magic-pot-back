namespace MagicPot.Backend.Attributes
{
    using System.ComponentModel.DataAnnotations;

    public class TonAddressAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            var address = value?.ToString();
            if (string.IsNullOrWhiteSpace(address))
            {
                return true;
            }

            return TonLibDotNet.Utils.AddressUtils.Instance.IsValid(address, out var _, out var _, out var _);
        }
    }
}
