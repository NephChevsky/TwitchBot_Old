using ApiDll;
using ChatDll;
using GoogleDll;
using ModelsDll;
using NLog;
using NLog.Extensions.Logging;
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
			string pathSecret = "secret.json";
			if (!File.Exists(pathSecret))
			{
				pathSecret = Path.Combine(@"D:\Dev\Twitch", pathSecret);
			}
			string pathConfig = "config.json";
			if (!File.Exists(pathConfig))
			{
				pathConfig = Path.Combine(@"D:\Dev\Twitch", pathConfig);
			}
			string pathBots = "bots.json";
			if (!File.Exists(pathBots))
			{
				pathBots = Path.Combine(@"D:\Dev\Twitch", pathBots);
			}
			builder.Configuration.SetBasePath(Directory.GetCurrentDirectory())
					.AddJsonFile("appsettings.json", false)
					.AddJsonFile(pathSecret, false)
					.AddJsonFile(pathConfig, false)
					.AddJsonFile(pathBots, false)
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
				config.Secret = builder.Configuration.GetSection("Secret").Get<Secret>().Twitch.ClientSecret;
			});

			builder.Services.Configure<HostOptions>(hostOptions =>
			{
				hostOptions.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
			});

			builder.Services.AddSingleton<Api>();
			builder.Services.AddSingleton<BasicChat>();
			builder.Services.AddSingleton<Spotify>();
			builder.Services.AddSingleton<DiscordDll.Discord>();
			builder.Services.AddSingleton<GoogleDll.Google>();
			builder.Services.AddHostedService<EventSubService>();
			builder.Services.AddHostedService<DailyTasks>();
			builder.Services.AddHostedService<ChronicTasks>();
			builder.Services.AddHostedService<CheckUptime>();
			builder.Services.AddHostedService<CheckChannelRewards>();
			builder.Services.AddHostedService<ChatService>();

			builder.Logging.ClearProviders();
			builder.Logging.AddNLog("nlog.config");
			GlobalDiagnosticsContext.Set("appName", "WebApp");

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