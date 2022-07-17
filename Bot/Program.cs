using Bot;
using Bot.Workers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Configuration;
using System.Runtime.Versioning;
using TwitchLib.EventSub.Webhooks;
using TwitchLib.EventSub.Webhooks.Extensions;

namespace Bot
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            })
            .ConfigureAppConfiguration((hostingContext, configuration) =>
            {
                configuration
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json")
                        .Build();
            })
            .ConfigureServices(services =>
            {
                //Settings settings = Configuration.GetSection(nameof(Settings)).Get<Settings>();

                services.AddSingleton<BotManager>();
                services.AddHostedService<CheckUptime>();
                services.AddHostedService<UpdateFiles>();
            })
            .Build();

            await host.RunAsync();
        }
    }
}