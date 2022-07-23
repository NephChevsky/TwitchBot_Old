using ModelsDll;
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

			builder.Services.AddHostedService<EventSubService>();

			builder.Logging.ClearProviders();
			builder.Logging.AddAzureWebAppDiagnostics();

			var app = builder.Build();

			if (!app.Environment.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
				app.UseHsts();
			}

			app.UseHttpsRedirection();
			app.UseStaticFiles();
			app.UseRouting();
			app.UseTwitchLibEventSubWebhooks();

			app.MapControllerRoute(
				name: "default",
				pattern: "{controller}/{action=Index}/{id?}");
			
			app.MapHub<SignalService>("/hub");

			app.MapFallbackToFile("index.html");

			app.Run();
		}
	}
}