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
            MainDb.CreateTable<PublishQueueItem>();

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

        public User GetOrCreateUser(long id, string? username, string? firstName, string? lastName, string? languageCode, bool isPremium, bool allowsWriteToPM)
        {
            var user = MainDb.Find<User>(id);

            var unchanged = user != null;

            user ??= new User() { Id = id, Created = DateTimeOffset.UtcNow };

            unchanged = unchanged
                && StringComparer.Ordinal.Equals(user.Username, username)
                && StringComparer.Ordinal.Equals(user.FirstName, firstName)
                && StringComparer.Ordinal.Equals(user.LastName, lastName)
                && StringComparer.Ordinal.Equals(user.LanguageCode, languageCode)
                && StringComparer.Ordinal.Equals(user.Username, username)
                && user.IsPremium == isPremium
                && user.AllowsWriteToPM == allowsWriteToPM;

            if (!unchanged)
            {
                user.Username = username;
                user.FirstName = firstName;
                user.LastName = lastName;
                user.LanguageCode = languageCode;
                user.IsPremium = isPremium;
                user.AllowsWriteToPM = allowsWriteToPM;
                MainDb.InsertOrReplace(user);
            }

            return user;
        }

        public User GetOrCreateUser(Attributes.InitDataValidationAttribute.InitDataUser user)
        {
            return GetOrCreateUser(user.Id, user.Username, user.FirstName, user.LastName, user.LanguageCode, user.IsPremium, user.AllowsWriteToPM);
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
            const int minVersion = 5;
            const int lastVersion = 7;

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

            if (ver == 5)
            {
                logger.LogInformation("Upgrading from version {Version}...", ver);
                connection.Execute("UPDATE [Pot] SET TxCount = 0 WHERE TxCount IS NULL");
                connection.InsertOrReplace(new Settings(Settings.KeyDbVersion, ++ver));
                logger.LogInformation("Upgraded to ver.{Version}", ver);
            }

            if (ver == 6)
            {
                logger.LogInformation("Upgrading from version {Version}...", ver);
                connection.Execute("ALTER TABLE [Pot] DROP COLUMN [State]");
                connection.InsertOrReplace(new Settings(Settings.KeyDbVersion, ++ver));
                logger.LogInformation("Upgraded to ver.{Version}", ver);
            }

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
