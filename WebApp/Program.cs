using ModelsDll;
using SpotifyDll;
using TwitchLib.EventSub.Webhooks.Extensions;
using WebApp.Services;

namespace WebApp
{
	public class Program
	{
		public static void Main(string[] args)
		{
			IConfigurationRoot configuration = new ConfigurationBuilder()
					.SetBasePath(Directory.GetCurrentDirectory())
					.AddJsonFile("appsettings.json")
					.Build();

			WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

			builder.Services.AddControllers();
			builder.Services.AddSignalR();
			builder.Services.AddTwitchLibEventSubWebhooks(config =>
			{
				config.CallbackPath = "/webhooks";
				config.Secret = configuration.GetSection("Settings").Get<Settings>().Secret;
			});

			builder.Services.AddSingleton<Spotify>();
			builder.Services.AddHostedService<EventSubService>();
			builder.Services.AddHostedService<UpdateButtons>();

			builder.Logging.ClearProviders();
			builder.Logging.AddAzureWebAppDiagnostics();

			var app = builder.Build();

			app.UseHttpsRedirection();
			app.UseStaticFiles();
			app.UseRouting();
			app.UseTwitchLibEventSubWebhooks();

			app.UseEndpoints(config =>
			{
				config.MapControllers();
			});
			
			app.MapHub<SignalService>("/hub");

			app.MapFallbackToFile("index.html");

			app.Run();
		}
	}
}