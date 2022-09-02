using ApiDll;
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
			string path = "config.json";
			if (!File.Exists(path))
			{
				path = Path.Combine(@"D:\Dev\Twitch", path);
			}
			builder.Configuration.SetBasePath(Directory.GetCurrentDirectory())
					.AddJsonFile("appsettings.json", false)
					.AddJsonFile(path, false)
					.AddJsonFile("bots.json", false)
					.Build();

			builder.Services.AddControllers();
			builder.Services.AddSignalR();
			builder.Services.AddCors(options =>
			{
				options.AddPolicy("devCORS",
				builder =>
				{
					builder.WithOrigins("https://localhost:44427")
						.AllowAnyHeader()
						.AllowCredentials();
				});
			});
			builder.Services.AddTwitchLibEventSubWebhooks(config =>
			{
				config.CallbackPath = "/webhooks";
				config.Secret = builder.Configuration.GetSection("Settings").Get<Settings>().Secret;
			});

			builder.Services.Configure<HostOptions>(hostOptions =>
			{
				hostOptions.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
			});

			builder.Services.AddSingleton<Api>();
			builder.Services.AddSingleton<Spotify>();
			builder.Services.AddSingleton<DiscordDll.Discord>();
			builder.Services.AddHostedService<EventSubService>();
			builder.Services.AddHostedService<DailyTasks>();
			builder.Services.AddHostedService<ChronicTasks>();

			builder.Logging.ClearProviders();
			builder.Logging.AddAzureWebAppDiagnostics();

			var app = builder.Build();

			app.UseHttpsRedirection();
			app.UseStaticFiles();
			app.UseRouting();
			if (app.Environment.EnvironmentName.Equals("Development"))
			{
				app.UseCors("devCORS");
			}
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