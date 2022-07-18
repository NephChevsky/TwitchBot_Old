using Bot.Services;
using Bot.Workers;
using Microsoft.AspNetCore.Hosting;

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
                services.AddSingleton<BotService>();
                services.AddSingleton<OBSService>();
                services.AddHostedService<CheckUptime>();
                services.AddHostedService<UpdateFiles>();
            })
            .Build();

            await host.RunAsync();
        }
    }
}