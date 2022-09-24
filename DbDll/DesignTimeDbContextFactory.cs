using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace DbDll
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TwitchDbContext>
    {
        public TwitchDbContext CreateDbContext(string[] args)
        {
            string path = "secret.json";
            if (!File.Exists(path))
            {
                path = Path.Combine(@"D:\Dev\Twitch", path);
            }
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(path, false)
                .Build();

            var connectionString = configuration.GetConnectionString(configuration.GetConnectionString("DbKey"));
            var builder = new DbContextOptionsBuilder<TwitchDbContext>();
            builder.UseSqlServer(connectionString);
            return new TwitchDbContext(builder.Options);
        }
    }
}
