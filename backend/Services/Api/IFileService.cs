namespace MagicPot.Backend.Services.Api
{
    public interface IFileService
    {
        Task<string> Upload(Stream data, string fileName);
    }
}
