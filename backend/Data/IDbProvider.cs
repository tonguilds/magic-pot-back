namespace MagicPot.Backend
{
    using SQLite;

    public interface IDbProvider : IDisposable
    {
        SQLiteConnection MainDb { get; }

        void Migrate();

        Task Reconnect();
    }
}
