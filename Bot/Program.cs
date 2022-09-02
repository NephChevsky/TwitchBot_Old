using ApiDll;
using Bot.Workers;
using ChatDll;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SpotifyDll;
using System.Diagnostics;

namespace Bot
{
	public class Program
	{
		static void Main(string[] args)
		{
            IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, configBuilder) =>
            {
                string path = "config.json";
                if (!File.Exists(path))
                {
                    path = Path.Combine(@"D:\dev\Twitch", path);
				}
                IConfigurationRoot config = configBuilder.SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", false)
                    .AddJsonFile(path, false)
                    .AddJsonFile("bots.json", false)
                    .Build();
            })
            .ConfigureServices(services =>
            {
                services.Configure<HostOptions>(hostOptions =>
                {
                    hostOptions.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
                });

                services.AddSingleton<Api>();
                services.AddSingleton<BasicChat>();
                services.AddSingleton<Spotify>();
                services.AddHostedService<ChatBot>();
                services.AddHostedService<CheckChannelRewards>();
            })
            .Build();

            host.Run();
        }
	}
}