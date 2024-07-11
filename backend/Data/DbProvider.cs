namespace MagicPot.Backend.Data
{
    using Microsoft.Extensions.Options;
    using SQLite;

    public class DbProvider : IDbProvider
    {
        public const int MaxLenKey = 20;
        public const int MaxLenName = 60;
        public const int MaxLenAddress = 100;
        public const int MaxLenUri = 1024;
        public const int MaxLen255 = 255;

        private readonly BackendOptions options;
        private readonly ILogger logger;

        private bool disposedValue;

        public DbProvider(IOptions<BackendOptions> options, ILogger<DbProvider> logger)
        {
            if (options?.Value == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            this.options = options.Value;
            MainDb = GetMainDb(this.options);
        }

        public SQLiteConnection MainDb { get; private set; }

        public void Migrate()
        {
            MainDb.CreateTable<Settings>();
            MainDb.CreateTable<Jetton>();
            MainDb.CreateTable<User>();
            MainDb.CreateTable<Pot>();
            MainDb.CreateTable<PotTransaction>();
            MainDb.CreateTable<PrecachedMnemonic>();
            MainDb.CreateTable<UserJettonWallet>();

            UpdateDb(MainDb);

            logger.LogInformation("Using {FilePath}", MainDb.DatabasePath);
        }

        public async Task Reconnect()
        {
            var olddb = MainDb;
            MainDb = GetMainDb(options);
            logger.LogDebug("Reconnected to DB");
            await Task.Delay(TimeSpan.FromSeconds(5));
            olddb.Close();
        }

        public User GetOrCreateUser(long id, string username)
        {
            var user = MainDb.Find<User>(id);

            if (user == null)
            {
                user = new User()
                {
                    Id = id,
                    Username = username,
                    Created = DateTimeOffset.UtcNow,
                };

                MainDb.Insert(user);
            }
            else if (!StringComparer.Ordinal.Equals(user.Username, username))
            {
                user.Username = username;
                MainDb.Update(user);
            }

            return user;
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    MainDb.Close();
                }

                disposedValue = true;
            }
        }

        protected SQLiteConnection GetMainDb(BackendOptions options)
        {
            var file = Path.GetFullPath(options.DatabaseFile);
            var conn = new SQLiteConnection(file);
            conn.EnableWriteAheadLogging();
            return conn;
        }

        protected void UpdateDb(SQLiteConnection connection)
        {
            const int minVersion = 1;
            const int lastVersion = 5;

            var ver = connection.Find<Settings>(Settings.KeyDbVersion)?.IntValue ?? 0;

            if (ver == 0)
            {
                ver = lastVersion;
                connection.Insert(new Settings(Settings.KeyDbVersion, ver));
            }

            if (ver < minVersion)
            {
                throw new InvalidOperationException($"Too old version: {ver} (supported minumum: {minVersion})");
            }

            ////if (ver == 1)
            ////{
            ////    logger.LogInformation("Upgrading from version {Version}...", ver);
            ////    connection.Execute("...");
            ////    connection.InsertOrReplace(new Settings(Settings.KeyDbVersion, ++ver));
            ////    logger.LogInformation("Upgraded to ver.{Version}", ver);
            ////}

            if (ver != lastVersion)
            {
                throw new UpdateDbException(ver, lastVersion);
            }
        }

        public class UpdateDbException(int actualVersion, int expectedVersion)
            : Exception($"Failed to update DB: actual version {actualVersion} does not equal to expected {expectedVersion}")
        {
            // Nothing
        }
    }
}
