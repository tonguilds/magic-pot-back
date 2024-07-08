namespace MagicPot.Backend.Services
{
    using Microsoft.Extensions.Options;

    public class PinataService(HttpClient httpClient, IOptions<PinataOptions> options)
        : IFileService
    {
        private const string Hostname = "api.pinata.cloud";

        public async Task<string> Upload(Stream data, string fileName)
        {
            using var file = new StreamContent(data);

            using var content = new MultipartFormDataContent
            {
                { file, "file", fileName },
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, $"https://{Hostname}/pinning/pinFileToIPFS");
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.Value.JwtToken);
            req.Content = content;

            using var resp = await httpClient.SendAsync(req);

            resp.EnsureSuccessStatusCode();

            var response = await resp.Content.ReadFromJsonAsync<PinFileResponse>();

            return $"https://{options.Value.GatewayDomain}/ipfs/{response!.IpfsHash}";
        }

        public class PinFileResponse
        {
            public string IpfsHash { get; set; } = string.Empty;

            public int PinSize { get; set; }

            public string Timestamp { get; set; } = string.Empty;

            public bool IsDuplicate { get; set; }
        }
    }
}
