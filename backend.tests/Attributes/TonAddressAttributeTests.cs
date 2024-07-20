namespace MagicPot.Backend.Attributes
{
    using Xunit;

    public class TonAddressAttributeTests
    {
        [Theory]
        [InlineData("EQCxE6mUtQJKFnGfaROTKOt1lZbDiiX1kCixRv7Nw2Id_sDs", true)]
        [InlineData("UQCxE6mUtQJKFnGfaROTKOt1lZbDiiX1kCixRv7Nw2Id_p0p", true)]
        [InlineData("kQCxE6mUtQJKFnGfaROTKOt1lZbDiiX1kCixRv7Nw2Id_ntm", true)]
        [InlineData("0QCxE6mUtQJKFnGfaROTKOt1lZbDiiX1kCixRv7Nw2Id_iaj", true)]
        [InlineData("0QCxE6mUtQJKFnGfaROTKOt1lZbDiiX1kCixRv7Nw2Id_000", false)]
        [InlineData("0QCxE6mUtQJKFnGfaROTKOt1lZbDiiX1kCixRv7Nw2Id_", false)]
        [InlineData("0QCxE6mUtQJKFnGfaROTKOt1lZbDiiX1kCixRv7Nw2Id_iaj000", false)]
        [InlineData("not an address", false)]
        public void IsWorks(string address, bool valid)
        {
            Assert.Equal(valid, new TonAddressAttribute().IsValid(address));
        }

        [Fact]
        public void EmptyStringAndNullAreOk()
        {
            Assert.True(new TonAddressAttribute().IsValid(string.Empty));
            Assert.True(new TonAddressAttribute().IsValid(null));
        }
    }
}
