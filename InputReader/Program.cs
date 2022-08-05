using ChatDll;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Windows.Forms;

namespace InputReader
{
	internal static class Program
	{
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