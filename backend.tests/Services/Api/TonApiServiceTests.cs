namespace MagicPot.Backend.Services.Api
{
    using Xunit;

    public class TonApiServiceTests : IDisposable
    {
        private readonly TonApiService service;
        private readonly HttpClient httpClient;

        private bool disposed;

        public TonApiServiceTests()
        {
            httpClient = new HttpClient();
            service = new TonApiService(httpClient);
        }

        [Fact]
        public async Task GetValidTestnetJetton()
        {
            service.InMainnet = false;
            var jetton = await service.GetJettonInfo("kQBbX2khki4ynoYWgXqmc7_5Xlcley9luaHxoSE0-7R2wqJA");

            Assert.NotNull(jetton);
            Assert.Equal("TonLib.Net Demo", jetton.Name);
            Assert.Equal("TLND", jetton.Symbol);
            Assert.Equal(9, jetton.Decimals);
            Assert.Equal("Demo jetton for TonLib.Net C# samples", jetton.Description);
            Assert.False(string.IsNullOrEmpty(jetton.Image));
        }

        [Fact]
        public async Task GetValidMainnetJetton()
        {
            service.InMainnet = true;
            var jetton = await service.GetJettonInfo("EQCxE6mUtQJKFnGfaROTKOt1lZbDiiX1kCixRv7Nw2Id_sDs");

            Assert.NotNull(jetton);
            Assert.Equal("Tether USD", jetton.Name);
            Assert.Equal("USD₮", jetton.Symbol);
            Assert.Equal(6, jetton.Decimals);
            Assert.Equal("Tether Token for Tether USD", jetton.Description);
            Assert.False(string.IsNullOrEmpty(jetton.Image));
        }

        [Theory]
        [InlineData("0QAkEWzRLi1sw9AlaGDDzPvk2_F20hjpTjlvsjQqYawVmTK7", false)]
        [InlineData("EQBkQP48aUEDg5Y5RRc8SxFHm_C5tNcJDlh3e9pYHC-ZmG2M", true)]
        public async Task TryGetInvalidJetton(string address, bool inMainnet)
        {
            service.InMainnet = inMainnet;
            var jetton = await service.GetJettonInfo(address);

            Assert.Null(jetton);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    httpClient?.Dispose();
                }

                disposed = true;
            }
        }
    }
}
