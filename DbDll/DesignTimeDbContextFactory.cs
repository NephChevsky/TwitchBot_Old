using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace DbDll
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TwitchDbContext>
    {
        public TwitchDbContext CreateDbContext(string[] args)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", false)
                    .Build();
            string path = configuration.GetValue<string>("ConfigPath");
            configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile(path, false)
                    .Build();

            var connectionString = configuration.GetConnectionString("AzureDb");
            var builder = new DbContextOptionsBuilder<TwitchDbContext>();
            builder.UseSqlServer(connectionString);
            return new TwitchDbContext(builder.Options);
        }
    }
}
