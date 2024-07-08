namespace MagicPot.Backend.Services
{
    public interface IFileService
    {
        Task<string> Upload(Stream data, string fileName);
    }
}
