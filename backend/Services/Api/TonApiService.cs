namespace MagicPot.Backend.Services.Api
{
    using MagicPot.Backend.Data;
    using MagicPot.Backend.Utils;

    public class TonApiService(HttpClient httpClient) : ITonApiService
    {
        private static DateTimeOffset lastRequest = DateTimeOffset.MinValue;
        private static int requestCount = 0;

        public bool InMainnet { get; set; } = Program.InMainnet;

        public string BaseUrl => InMainnet ? "tonapi.io" : "testnet.tonapi.io";

        public async Task<Jetton?> GetJettonInfo(string address)
        {
            await WaitIfNeeded();

            using var resp = await httpClient.GetAsync($"https://{BaseUrl}/v2/jettons/{address}");

            if (resp.IsSuccessStatusCode)
            {
                var info = await resp.Content.ReadFromJsonAsync<JettonInfo>();
                if (info != null && info.metadata != null)
                {
                    return new Jetton()
                    {
                        Address = AddressConverter.ToContract(address),
                        Name = info.metadata.name,
                        Symbol = info.metadata.symbol,
                        Decimals = info.metadata.decimals,
                        Image = info.metadata.image,
                        Description = info.metadata.description,
                    };
                }
            }

            return null;
        }

        private static async ValueTask WaitIfNeeded()
        {
            if (DateTimeOffset.UtcNow.Subtract(lastRequest) > TimeSpan.FromSeconds(2))
            {
                lastRequest = DateTimeOffset.UtcNow;
                requestCount = 0;
            }
            else if (requestCount > 2)
            {
                await Task.Delay(1000);
            }
            else
            {
                Interlocked.Increment(ref requestCount);
            }
        }

#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable SA1516 // Elements should be separated by blank line

        public class JettonInfo
        {
            public bool mintable { get; set; }
            public string total_supply { get; set; } = string.Empty;
            public Admin admin { get; set; } = new();
            public Metadata? metadata { get; set; }
            public string verification { get; set; } = string.Empty;
            public int holders_count { get; set; }

            public class Admin
            {
                public string address { get; set; } = string.Empty;
                public bool is_scam { get; set; }
                public bool is_wallet { get; set; }
            }

            public class Metadata
            {
                public string address { get; set; } = string.Empty;
                public string name { get; set; } = string.Empty;
                public string symbol { get; set; } = string.Empty;
                public byte decimals { get; set; }
                public string image { get; set; } = string.Empty;
                public string description { get; set; } = string.Empty;
            }
        }
    }
}
