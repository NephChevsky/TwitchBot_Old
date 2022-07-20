using Bot.Workers;
using ChatDll;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
                services.AddSingleton<Chat>();
                services.AddHostedService<CheckUptime>();
                services.AddHostedService<UpdateFiles>();
            })
            .Build();

            host.Run();
        }
	}
}