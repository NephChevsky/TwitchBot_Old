using ApiDll;
using ChatDll;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using ObsDll;
using SpeechDll;
using SpotifyDll;

namespace StreamDeck
{
	public class Program
	{
		static void Main(string[] args)
		{
            IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, configBuilder) =>
            {
                string pathSecret = "secret.json";
                if (!File.Exists(pathSecret))
                {
                    pathSecret = Path.Combine(@"D:\dev\Twitch", pathSecret);
                }
                string pathConfig = "config.json";
                if (!File.Exists(pathConfig))
                {
                    pathConfig = Path.Combine(@"D:\dev\Twitch", pathConfig);
				}
                IConfigurationRoot config = configBuilder.SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", false)
                    .AddJsonFile(pathSecret, false)
                    .AddJsonFile(pathConfig, false)
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
                services.AddSingleton<Obs>();
                services.AddSingleton<Speech>();
                services.AddHostedService<Workers.StreamDeck>();

                services.AddLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddNLog("nlog.config");
                    GlobalDiagnosticsContext.Set("appName", "StreamDeck");
                });
            })
            .Build();

            host.Run();
        }
	}
}