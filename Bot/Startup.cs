using Bot.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using TwitchLib.EventSub.Webhooks.Extensions;

namespace Bot
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddSignalR();
            services.AddCors(options =>
            {
                options.AddPolicy("devCORS",
                builder =>
                {
                    builder.WithOrigins("http://localhost:4200")
                        .AllowAnyHeader()
                        .AllowCredentials();
                });
            });
            services.AddTwitchLibEventSubWebhooks(config =>
            {
                config.CallbackPath = "/webhooks";
                config.Secret = "supersecret";
            });

            services.AddHostedService<EventSubService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            if (env.EnvironmentName.Equals("Development"))
                app.UseCors("devCORS");

            app.UseAuthorization();

            app.UseTwitchLibEventSubWebhooks();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<SignalService>("/hub");
            });
        }
    }
}
