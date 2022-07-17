using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Db
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TwitchDbContext>
    {
        public TwitchDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<TwitchDbContext>();
            builder.UseSqlServer("Server=localhost;Database=TheCompany;Trusted_Connection=True;");
            return new TwitchDbContext(builder.Options);
        }
    }
}
