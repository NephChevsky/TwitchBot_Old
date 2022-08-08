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
			WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

			builder.Configuration.SetBasePath(Directory.GetCurrentDirectory())
					.AddJsonFile("appsettings.json", false)
					.AddJsonFile("config.json", false)
					.Build();

			builder.Services.AddControllers();
			builder.Services.AddSignalR();
			builder.Services.AddTwitchLibEventSubWebhooks(config =>
			{
				config.CallbackPath = "/webhooks";
				config.Secret = builder.Configuration.GetSection("Settings").Get<Settings>().Secret;
			});

			builder.Services.AddSingleton<Spotify>();
			builder.Services.AddSingleton<DiscordDll.Discord>();
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