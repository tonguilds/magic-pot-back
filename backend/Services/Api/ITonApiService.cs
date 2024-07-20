namespace MagicPot.Backend.Services.Api
{
    using MagicPot.Backend.Data;

    public interface ITonApiService
    {
        Task<Jetton?> GetJettonInfo(string address);
    }
}
