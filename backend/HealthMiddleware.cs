namespace MagicPot.Backend
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Text.Json;
    using System.Threading.Tasks;
    using MagicPot.Backend.Services.Api;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;

    public class HealthMiddleware : IMiddleware
    {
        private const string HealthCode = "487WKZQAHDHQMHRNWTJR"; // Random string

        public Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            context = context ?? throw new ArgumentNullException(nameof(context));

            return context.Request.Path.ToString() switch
            {
                "/health" => WriteHtml(context),
                "/health.json" => WriteJson(context),
                _ => next.Invoke(context),
            };
        }

        private async Task WriteHtml(HttpContext context)
        {
            var response = context.Response;
            response.StatusCode = StatusCodes.Status200OK;
            response.ContentType = "text/html; charset=utf-8";
            response.Headers.CacheControl = "no-store,no-cache";

            using var stream = new MemoryStream();
            using (var writer = new StreamWriter(stream, leaveOpen: true))
            {
#pragma warning disable S6966 // We use MemoryStream, no need to use async
                writer.WriteLine("<html><body><h1>Health check page</h1><dl>");

                foreach (var (key, value) in GetValues(context))
                {
                    writer.WriteLine("<dt>" + key + "</dt><dd>" + value + "</dd>");
                }

                writer.WriteLine("</dl></html>");
#pragma warning restore S6966 // Awaitable method should be used
            }

            stream.Position = 0;
            await stream.CopyToAsync(response.Body).ConfigureAwait(false);
        }

        private async Task WriteJson(HttpContext context)
        {
            var response = context.Response;
            response.StatusCode = StatusCodes.Status200OK;
            response.ContentType = "application/json";
            response.Headers.CacheControl = "no-store,no-cache";

            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();
                foreach (var (key, value) in GetValues(context))
                {
                    writer.WritePropertyName(key);
                    JsonSerializer.Serialize(writer, value, value.GetType());
                }

                writer.WriteEndObject();
            }

            stream.Position = 0;
            await stream.CopyToAsync(response.Body).ConfigureAwait(false);
        }

        private IEnumerable<(string Key, object Value)> GetValues(HttpContext context)
        {
            var allOk = true;

            var appAssembly = this.GetType().Assembly;
            var appTitle = appAssembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title ?? appAssembly.GetName().Name ?? "Unknown :(";
            var appVersion = appAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? appAssembly.GetName().Version?.ToString() ?? "Unknown";

            yield return ("App Title", appTitle);
            yield return ("App Version", appVersion);

            var dotnetVersion = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
            yield return (".NET version", dotnetVersion);

            var aspAssembly = typeof(HttpContext).Assembly;
            var aspVersion = aspAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? aspAssembly.GetName().Version?.ToString() ?? "Unknown";
            yield return ("ASP.NET Core version", aspVersion);

            var inContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER");
            yield return ("In container", inContainer ?? "no");

            yield return ("Now", DateTimeOffset.UtcNow.ToString("u", CultureInfo.InvariantCulture));

            yield return ("Your IP", context.Connection.RemoteIpAddress?.ToString() ?? "-unknown-");

            ////static (string Key, string Text) DescribeEntities(string code, IEnumerable<IBlockchainEntity> items)
            ////{
            ////    var count = items.Count();
            ////    var text = count == 0
            ////        ? "no entities"
            ////        : $"total {count}, min sync {items.Min(x => x.LastSync):u}, max sync {items.Max(x => x.LastSync):u}";
            ////    return ($"Entity {code}", text);
            ////}

            var cd = context.RequestServices.GetRequiredService<CachedData>();
            ////yield return DescribeEntities("A", cd.AllAdmins);
            ////yield return DescribeEntities("U", cd.AllUsers);
            ////yield return DescribeEntities("O", cd.AllOrders);

            yield return ("Entity J", cd.KnownJettons.Count);

            yield return ("Masterchain seqno", cd.LastKnownSeqno.ToString(CultureInfo.InvariantCulture));

            var ownTasks = StartupApi.RegisteredTasks
                .Select(x =>
                {
                    var task = (RecurrentTasks.ITask)context.RequestServices.GetRequiredService(x);
                    return new TaskInfo
                    {
                        Name = x.GenericTypeArguments[0].Name,
                        Interval = task.Options.Interval,
                        FailsCount = task.RunStatus.FailsCount,
                        LastExceptionName = task.RunStatus.LastException?.GetType().Name,
                        LastSuccessTime = task.RunStatus.LastSuccessTime,
                    };
                })
                .ToList();

            foreach (var task in ownTasks.Concat(IndexerHealthUpdateMiddleware.Data))
            {
                if (task.Interval <= TimeSpan.Zero)
                {
                    task.Name += " (stopped)";
                    if (task.FailsCount > 0)
                    {
                        allOk = false;
                        yield return (task.Name, $"failed with {task.LastExceptionName}, last success {task.LastSuccessTime:u}");
                    }
                    else
                    {
                        yield return (task.Name, task.LastSuccessTime.ToString("u"));
                    }
                }
                else
                {
                    // Wait twice default interval, but not less than 3 minutes.
                    var allowed = Math.Max(task.Interval.TotalSeconds * 2, 60 * 3);
                    if (task.LastSuccessTime.AddSeconds(allowed) < DateTimeOffset.Now)
                    {
                        allOk = false;
                        yield return (task.Name, $"failed with {task.LastExceptionName}, last success {task.LastSuccessTime:u}");
                    }
                    else
                    {
                        yield return (task.Name, task.LastSuccessTime.ToString("u"));
                    }
                }
            }

            yield return ("Healthy", allOk ? $"Yes, code {HealthCode}" : "NO");
        }

        public class TaskInfo
        {
            public required string Name { get; set; }

            public required TimeSpan Interval { get; set; }

            public required int FailsCount { get; set; }

            public required string? LastExceptionName { get; set; }

            public required DateTimeOffset LastSuccessTime { get; set; }
        }
    }
}
