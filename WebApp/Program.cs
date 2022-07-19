using ChatDll;
using WebApp.Workers;

namespace WebApp
{
	public class Program
	{
		public static void Main(string[] args)
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
                services.AddSingleton<Chat>();
                services.AddHostedService<CheckUptime>();
            })
            .Build();

            host.Run();
        }
	}
}