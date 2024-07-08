namespace MagicPot.Bot
{
    using System;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    public class Startup(IConfiguration configuration)
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<RobotsTxtMiddleware>();

            var bos = configuration.GetSection("BotOptions");
            services.Configure<BotOptions>(bos);
            var bo = new BotOptions();
            bos.Bind(bo);

            services.AddTelegramBot<MagicPotBot>(bo.Token, bo.WebhookUrl);
            services.AddTelegramBotCommandHandlers(typeof(Startup).Assembly);
            services.AddTask<InitialConnectionTask>(o => o.AutoStart(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5)));
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseTelegramBot<MagicPotBot>();

            app.UseMiddleware<RobotsTxtMiddleware>();

            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("<html><body><h1>Nothing here, sorry.</h1></body></html>");
            });
        }
    }
}
