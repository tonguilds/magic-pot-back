namespace MagicPot.Backend.Services.Indexer
{
    using System.Text.Json;
    using MagicPot.Backend;
    using Microsoft.Extensions.Options;
    using RecurrentTasks;

    public class HealthReportTask : IRunnable
    {
        public static readonly TimeSpan DefaultInterval = TimeSpan.FromSeconds(9);

        private readonly ILogger logger;
        private readonly IConfiguration configuration;
        private readonly string path;
        private readonly IHttpClientFactory httpClientFactory;

        public HealthReportTask(ILogger<HealthReportTask> logger, IConfiguration configuration, IOptions<BackendOptions> backendOptions, IHttpClientFactory httpClientFactory)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            path = backendOptions.Value.HealthReportPath;
            this.httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        public async Task RunAsync(ITask currentTask, IServiceProvider scopeServiceProvider, CancellationToken cancellationToken)
        {
            var data = GetData(scopeServiceProvider);

            var baseUrl = configuration["Kestrel:Endpoints:Http:Url"];
            var uri = baseUrl + path;

            logger.LogTrace("Sending POST to {Uri}", uri);

            using var httpClient = httpClientFactory.CreateClient();
            using var resp = await httpClient.PostAsJsonAsync(uri, data, JsonSerializerOptions.Default, cancellationToken);
            resp.EnsureSuccessStatusCode();
        }

        private static List<HealthMiddleware.TaskInfo> GetData(IServiceProvider scopeServiceProvider)
        {
            return StartupIndexer.RegisteredTasks
                .Select(x =>
                {
                    var task = (ITask)scopeServiceProvider.GetRequiredService(x);
                    return new HealthMiddleware.TaskInfo
                    {
                        Name = x.GenericTypeArguments[0].Name,
                        Interval = task.Options.Interval,
                        FailsCount = task.RunStatus.FailsCount,
                        LastExceptionName = task.RunStatus.LastException?.GetType().Name,
                        LastSuccessTime = task.RunStatus.LastSuccessTime,
                    };
                })
                .ToList();
        }
    }
}
