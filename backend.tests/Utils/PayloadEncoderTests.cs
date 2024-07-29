namespace MagicPot.Backend.Utils
{
    using Xunit;

    public class PayloadEncoderTests
    {
        [Theory]
        [InlineData(8999606968112862966, 3493193318311815107)]
        [InlineData(8953607954017309463, 171010220180550209)]
        [InlineData(1053268606363225696, 3834284997674047506)]
        public void CanDecodeEncoded(long potId, long userId)
        {
            var bytes = PayloadEncoder.Encode(potId, userId);
            Assert.NotNull(bytes);
            Assert.NotEmpty(bytes);
            Assert.True(PayloadEncoder.TryDecode(bytes, out var p, out var u));
            Assert.Equal(potId, p);
            Assert.Equal(userId, u);
        }

        [Fact]
        public void BytesAreInCorrectOrder()
        {
            var potId = 0x56e28de6877dc812;
            var userId = 0x7cb9d3ab22942101;

            var bytes = PayloadEncoder.Encode(potId, userId);

            var expected = new byte[]
            {
                0x2a, 0x56, 0x7c,
                0xb0, 0xe2, 0xb9,
                0x6b, 0x8d, 0xd3,
                0x49, 0xe6, 0xab,
                0xd4, 0x87, 0x22,
                0x3e, 0x7d, 0x94,
                0xbd, 0xc8, 0x21,
                0x3b, 0x12, 0x01,
            };

            Assert.True(PayloadEncoder.TryDecode(bytes, out var p, out var u));

            Assert.Equal(expected, bytes);
            Assert.Equal(potId, p);
            Assert.Equal(userId, u);
        }

        [Fact]
        public void FailsWhenEmpty()
        {
            var bytes = Array.Empty<byte>();
            Assert.False(PayloadEncoder.TryDecode(bytes, out var _, out var _));
        }

        [Fact]
        public void FailsWhenShort()
        {
            var bytes = new byte[] { 1, 2, 3 };
            Assert.False(PayloadEncoder.TryDecode(bytes, out var _, out var _));
        }

        [Fact]
        public void FailsWhenLong()
        {
            var bytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25 };
            Assert.False(PayloadEncoder.TryDecode(bytes, out var _, out var _));
        }

        [Fact]
        public void FailsWhenInvalidPayload()
        {
            var bytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 };
            Assert.False(PayloadEncoder.TryDecode(bytes, out var _, out var _));
        }
    }
}
