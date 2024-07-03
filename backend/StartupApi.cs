namespace MagicPot.Backend
{
    using System.Reflection;
    using System.Text.Json.Serialization;
    using MagicPot.Backend.Services;
    using Microsoft.OpenApi.Models;
    using RecurrentTasks;

    public class StartupApi(IConfiguration configuration)
    {
        public static IReadOnlyList<Type> RegisteredTasks { get; private set; } = [];

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<RobotsTxtMiddleware>();

            services
                .AddControllers()
                .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase, allowIntegerValues: false)));

            var optionsSection = configuration.GetSection("BackendOptions");
            services.Configure<BackendOptions>(optionsSection);

            var bo = new BackendOptions();
            optionsSection.Bind(bo);

            services.AddScoped<IDbProvider, DbProvider>();
            services.AddScoped(sp => new Lazy<IDbProvider>(() => sp.GetRequiredService<IDbProvider>()));

            services.AddSingleton<CachedData>();
            services.AddTask<CachedData>(o => o.AutoStart(bo.CacheReloadInterval, TimeSpan.FromSeconds(1)));

            services.AddTask<IndexerControlTask>(o => o.AutoStart(IndexerControlTask.Interval, TimeSpan.FromSeconds(5)), ServiceLifetime.Singleton);

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
                ];
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseForwardedHeaders(new() { ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.All });
            app.UseStatusCodePages();

            app.UseExceptionHandler(ab => ab.Run(ctx =>
            {
                ctx.Response.ContentType = "text/plain";
                return ctx.Response.WriteAsync($"Nothing here. Please enjoy StatusCode {ctx.Response.StatusCode}.");
            }));

            app.UseMiddleware<RobotsTxtMiddleware>();
            ////app.UseMiddleware<HealthMiddleware>();
            ////app.UseMiddleware<SearchCacheUpdateMiddleware>();
            ////app.UseMiddleware<IndexerHealthUpdateMiddleware>();

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
