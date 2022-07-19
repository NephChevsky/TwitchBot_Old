using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DbDll
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TwitchDbContext>
    {
        public TwitchDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<TwitchDbContext>();
            builder.UseSqlServer("Server=NEPH-DESKTOP\\SQLEXPRESS;Database=Twitch;Trusted_Connection=True;Connect Timeout=10");
            return new TwitchDbContext(builder.Options);
        }
    }
}
