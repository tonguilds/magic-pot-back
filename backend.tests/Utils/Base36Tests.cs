namespace MagicPot.Backend
{
    using Xunit;

    public class Base36Tests
    {
        [Theory]
        [InlineData(0, "0")]
        [InlineData(50, "1e")]
        [InlineData(100, "2s")]
        [InlineData(999, "rr")]
        [InlineData(1000, "rs")]
        [InlineData(1111, "uv")]
        [InlineData(5959, "4lj")]
        [InlineData(99999, "255r")]
        [InlineData(123456789, "21i3v9")]
        [InlineData(int.MaxValue, "zik0zj")]
        public void EncodeAndDecodeInt(int value, string encoded)
        {
            Assert.Equal(encoded, Base36.Encode(value));
            Assert.Equal(value, Base36.DecodeInt(encoded));
        }

        [Theory]
        [InlineData(5481594952936519619, "15n9z8l3au4eb")]
        [InlineData(long.MaxValue, "1y2p0ij32e8e7")]
        public void EncodeAndDecodeLong(long value, string encoded)
        {
            Assert.Equal(encoded, Base36.Encode(value));
            Assert.Equal(value, Base36.DecodeLong(encoded));
        }
    }
}
