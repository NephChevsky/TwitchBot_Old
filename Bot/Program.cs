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
            .ConfigureAppConfiguration((hostingContext, configuration) =>
            {
                configuration
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json")
                        .Build();
            })
            .ConfigureServices(services =>
            {
                services.AddSingleton<BasicChat>();
                services.AddSingleton<ChatBot>();
                services.AddSingleton<Spotify>();
                services.AddHostedService<CheckUptime>();
            })
            .Build();

            Process.Start(new ProcessStartInfo
            {
                FileName = "https://accounts.spotify.com/authorize?client_id=e3e3047455fb4f85b889cc251133b9c9&response_type=code&redirect_uri=https%3A%2F%2Fbot-neph.azurewebsites.net%2Fcallback&scope=user-read-currently-playing+user-modify-playback-state+playlist-modify-public+playlist-modify-private",
                UseShellExecute = true
            });

            host.Run();
        }
	}
}