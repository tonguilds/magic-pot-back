namespace MagicPot.Backend.Data
{
    using SQLite;

    public interface IDbProvider : IDisposable
    {
        SQLiteConnection MainDb { get; }

        void Migrate();

        Task Reconnect();

        User GetOrCreateUser(long id, string username);
    }
}
