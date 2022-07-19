using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace DbDll
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TwitchDbContext>
    {
        public TwitchDbContext CreateDbContext(string[] args)
        {
            string dir = Directory.GetCurrentDirectory();
            if (!dir.Contains("WebApp"))
                dir = Path.Combine(Directory.GetParent(dir).FullName, "WebApp");

            IConfigurationRoot configuration = new ConfigurationBuilder()
                    .SetBasePath(dir)
                    .AddJsonFile("appsettings.json")
                    .Build();

            var connectionString = configuration.GetConnectionString("AzureDb");
            var builder = new DbContextOptionsBuilder<TwitchDbContext>();
            builder.UseSqlServer(connectionString);
            return new TwitchDbContext(builder.Options);
        }
    }
}
