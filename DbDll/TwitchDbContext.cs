using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Configuration;
using ModelsDll.Db;
using ModelsDll.Interfaces;

namespace DbDll
{
    public partial class TwitchDbContext : DbContext
    {
        public TwitchDbContext(DbContextOptions options) : base(options)
        {
        }

        public TwitchDbContext()
        {
        }

        public DbSet<Viewer> Viewers => Set<Viewer>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            string dir = Directory.GetCurrentDirectory();
            if (!dir.Contains("WebApp"))
                dir = Path.Combine(Directory.GetParent(dir).FullName, "WebApp");
            IConfigurationRoot configuration = new ConfigurationBuilder()
                    .SetBasePath(dir)
                    .AddJsonFile("appsettings.json")
                    .Build();

            var connectionString = configuration.GetConnectionString("AzureDb");
            optionsBuilder.UseSqlServer(connectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Viewer>(entity =>
            {
                entity.Property(e => e.Username)
                    .IsRequired()
                    .HasMaxLength(512);

                entity.Property(e => e.TwitchId)
                    .HasMaxLength(512);

                entity.Property(e => e.IsBot)
                    .IsRequired()
                    .HasDefaultValue(false);

                entity.Property(e => e.Seen)
                    .IsRequired()
                    .HasDefaultValue(0);

                entity.Property(e => e.LastViewedDateTime)
                    .IsRequired();

                entity.Property(e => e.Uptime)
                    .IsRequired()
                    .HasDefaultValue(0);

                AddGenericFields<Viewer>(entity);
            });

            modelBuilder.Entity<Viewer>().HasIndex(t => new { t.Id }).IsUnique(true);
            modelBuilder.Entity<Viewer>().HasIndex(t => new { t.Username }).IsUnique(true);
        }

        public void AddGenericFields<T>(EntityTypeBuilder entity)
        {
            entity.Property("Id")
                  .ValueGeneratedOnAdd();

            if (typeof(IDateTimeTrackable).IsAssignableFrom(typeof(T)))
            {
                entity.Property("CreationDateTime")
                   .IsRequired();

                entity.Property("LastModificationDateTime");
            }

            if (typeof(ISoftDeleteable).IsAssignableFrom(typeof(T)))
            {
                entity.Property("Deleted")
                    .IsRequired()
                    .HasDefaultValue(false);
            }
        }

        public override int SaveChanges()
        {
            SoftDelete();
            TimeTrack();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            SoftDelete();
            TimeTrack();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void SoftDelete()
        {
            ChangeTracker.DetectChanges();
            var markedAsDeleted = ChangeTracker.Entries().Where(x => x.State == EntityState.Deleted);
            foreach (var item in markedAsDeleted)
            {
                if (item.Entity is ISoftDeleteable entity)
                {
                    item.State = EntityState.Unchanged;
                    entity.Deleted = true;
                }
            }
        }

        private void TimeTrack()
        {
            ChangeTracker.DetectChanges();
            var markedEntries = ChangeTracker.Entries().Where(x => x.State == EntityState.Added || x.State == EntityState.Modified);
            DateTime now = DateTime.Now;
            foreach (var item in markedEntries)
            {
                if (item.Entity is IDateTimeTrackable entity)
                {
                    entity.LastModificationDateTime = now;
                    if (item.State == EntityState.Added && (entity.CreationDateTime == null || entity.CreationDateTime == DateTime.MinValue))
                    {
                        entity.CreationDateTime = now;
                    }
                }
            }
        }

    }
}