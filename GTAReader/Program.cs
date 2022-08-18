using ChatDll;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GTAReader
{
	internal static class Program
	{
		/// <summary>
		///  The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
            IHost host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((hostingContext, configBuilder) =>
            {
                IConfigurationRoot config = configBuilder
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", false)
                    .Build();
                string path = config.GetValue<string>("ConfigPath");
                configBuilder.SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", false)
                    .AddJsonFile(path, false)
                    .Build();
            })
            .ConfigureServices(services =>
            {
                services.AddSingleton<BasicChat>();
                services.AddHostedService<InputService>();
            })
            .Build();

            host.Run();
        }
	}
}