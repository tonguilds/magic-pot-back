namespace MagicPot.Backend
{
    using System.Reflection;
    using System.Text.Json.Serialization;
    using MagicPot.Backend.Data;
    using MagicPot.Backend.Services;
    using MagicPot.Backend.Services.Api;
    using MagicPot.Backend.Utils;
    using Microsoft.OpenApi.Models;
    using Polly;
    using Polly.Extensions.Http;
    using RecurrentTasks;

    public class StartupApi(IConfiguration configuration)
    {
        public static IReadOnlyList<Type> RegisteredTasks { get; private set; } = [];

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<RobotsTxtMiddleware>();
            services.AddSingleton<HealthMiddleware>();
            services.AddSingleton<IndexerHealthUpdateMiddleware>();

            services
                .AddControllers(o => o.InputFormatters.Add(new PlainTextFormatter()))
                .ConfigureApiBehaviorOptions(o => o.SuppressModelStateInvalidFilter = true)
                .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase, allowIntegerValues: false)));

            var optionsSection = configuration.GetSection("BackendOptions");
            services.Configure<BackendOptions>(optionsSection);
            var bo = optionsSection.Get<BackendOptions>()!;

            services.AddScoped<IDbProvider, DbProvider>();
            services.AddScoped(sp => new Lazy<IDbProvider>(() => sp.GetRequiredService<IDbProvider>()));

            services.AddSingleton<CachedData>();
            services.AddTask<CachedData>(o => o.AutoStart(bo.CacheReloadInterval, TimeSpan.FromSeconds(1)));

            services.AddTask<IndexerControlTask>(o => o.AutoStart(IndexerControlTask.Interval, TimeSpan.FromSeconds(5)), ServiceLifetime.Singleton);

            // Required for correct DB initialization (e.g. UseMainnet flag)
            services.Configure<TonLibDotNet.TonOptions>(configuration.GetSection("TonOptions"));

            services.AddHttpClient<ITonApiService, TonApiService>(c => c.Timeout = TimeSpan.FromSeconds(60))
                .AddPolicyHandler(Policy.WrapAsync(
                    HttpPolicyExtensions.HandleTransientHttpError().Or<Polly.Timeout.TimeoutRejectedException>().WaitAndRetryAsync(3, x => TimeSpan.FromSeconds(1)),
                    Policy.TimeoutAsync<HttpResponseMessage>(10)));
            services.AddScoped(sp => new Lazy<ITonApiService>(() => sp.GetRequiredService<ITonApiService>()));

            services.Configure<PinataOptions>(configuration.GetSection("PinataOptions"));
            services.AddHttpClient<IFileService, PinataService>(c => c.Timeout = TimeSpan.FromSeconds(120))
                .AddPolicyHandler(Policy.WrapAsync(
                    HttpPolicyExtensions.HandleTransientHttpError().Or<Polly.Timeout.TimeoutRejectedException>().WaitAndRetryAsync(3, x => TimeSpan.FromSeconds(x)),
                    Policy.TimeoutAsync<HttpResponseMessage>(10)));
            services.AddScoped(sp => new Lazy<IFileService>(() => sp.GetRequiredService<IFileService>()));

            services.Configure<BackupOptions>(configuration.GetSection("BackupOptions"));
            services.AddTask<BackupTask>(o => o.AutoStart = true);

            services.AddSingleton<INotificationService, NotificationService>();

            services.AddHttpClient<ScheduledMessageSender>(c => c.Timeout = TimeSpan.FromSeconds(60))
                .AddPolicyHandler(Policy.WrapAsync(
                    HttpPolicyExtensions.HandleTransientHttpError().Or<Polly.Timeout.TimeoutRejectedException>().WaitAndRetryAsync(3, x => TimeSpan.FromSeconds(x)),
                    Policy.TimeoutAsync<HttpResponseMessage>(5)));
            services.AddTask<ScheduledMessageSender>(o => o.AutoStart(ScheduledMessageSender.DefaultInterval));

            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(o =>
            {
                o.SupportNonNullableReferenceTypes();
                o.SwaggerDoc("backend", new OpenApiInfo()
                {
                    Title = "Magic Pot API",
                    Description = "Backend for Magic Pot.",
                    Version = "backend",
                });
                o.EnableAnnotations();

                var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                o.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
            });

            services.AddCors(o =>
            {
                o.AddDefaultPolicy(
                    builder => builder
                        .AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod());
            });

            RegisteredTasks =
                [
                    typeof(ITask<CachedData>),
                    typeof(ITask<IndexerControlTask>),
                    typeof(ITask<BackupTask>),
                    typeof(ITask<ScheduledMessageSender>),
                ];
        }

        public void Configure(IApplicationBuilder app, INotificationService notificationService)
        {
            app.UseForwardedHeaders(new() { ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.All });
            app.UseStatusCodePages();

            app.UseExceptionHandler(ab => ab.Run(ctx =>
            {
                ctx.Response.ContentType = "text/plain";
                return ctx.Response.WriteAsync($"Nothing here. Please enjoy StatusCode {ctx.Response.StatusCode}.");
            }));

            app.UseMiddleware<RobotsTxtMiddleware>();
            app.UseMiddleware<HealthMiddleware>();
            app.UseMiddleware<IndexerHealthUpdateMiddleware>();

            app.UseSwagger();
            app.UseSwaggerUI(o => o.SwaggerEndpoint("/swagger/backend/swagger.json", "Magic Pot API"));

            app.UseRouting();
            app.UseCors();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
