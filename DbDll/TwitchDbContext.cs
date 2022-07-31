using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Configuration;
using ModelsDll.Db;
using ModelsDll.Interfaces;
using System.Linq.Expressions;

namespace DbDll
{
    public partial class TwitchDbContext : DbContext
    {
        private Guid Owner;

        public TwitchDbContext(DbContextOptions options) : base(options)
        {
        }

        public TwitchDbContext(Guid owner)
        {
            Owner = owner;
        }

        public DbSet<Viewer> Viewers => Set<Viewer>();
        public DbSet<Command> Commands => Set<Command>();
        public DbSet<ChatMessage> Messages => Set<ChatMessage>();
        public DbSet<Uptime> Uptimes => Set<Uptime>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            string dir = Directory.GetCurrentDirectory();
            if (!dir.Contains("wwwroot") && !dir.Contains("Bot"))
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

                entity.Property(e => e.DisplayName)
                    .IsRequired()
                    .HasMaxLength(512);

                entity.Property(e => e.TwitchId)
                    .IsRequired()
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

                entity.Property(e => e.MessageCount)
                    .IsRequired()
                    .HasDefaultValue(0);

                AddGenericFields<Viewer>(entity);
            });
            modelBuilder.Entity<Viewer>().HasIndex(t => new { t.Id }).IsUnique(true);
            modelBuilder.Entity<Viewer>().HasIndex(t => new { t.Username }).HasFilter($"{nameof(Viewer.Deleted)} = 0").IsUnique(true);

            modelBuilder.Entity<Command>(entity =>
            {
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(512);

                entity.Property(e => e.Message)
                    .IsRequired()
                    .HasMaxLength(512);

                entity.Property(e => e.Value)
                    .HasDefaultValue(0);

                AddGenericFields<Command>(entity);
            });
            modelBuilder.Entity<Viewer>().HasIndex(t => new { t.Id }).IsUnique(true);
            modelBuilder.Entity<Command>().HasIndex(t => new { t.Name }).HasFilter($"{nameof(Command.Deleted)} = 0").IsUnique(true);

            modelBuilder.Entity<ChatMessage>(entity =>
            {
                entity.Property(e => e.Message)
                    .IsRequired();

                AddGenericFields<ChatMessage>(entity);
            });
            modelBuilder.Entity<ChatMessage>().HasIndex(t => new { t.Id }).IsUnique(true);

            modelBuilder.Entity<Uptime>(entity =>
            {
                entity.Property(e => e.Sum)
                    .HasDefaultValue(0)
                    .IsRequired();

                AddGenericFields<Uptime>(entity);
            });
            modelBuilder.Entity<Uptime>().HasIndex(t => new { t.Id }).IsUnique(true);

            Expression<Func<ISoftDeleteable, bool>> filterSoftDeleteable = bm => !bm.Deleted;
            Expression<Func<IOwnable, bool>> filterOwnable = bm => Owner == Guid.Empty || bm.Owner == Owner;
            foreach (var type in modelBuilder.Model.GetEntityTypes())
            {
                Expression filter = null;
                var param = Expression.Parameter(type.ClrType, "entity");
                if (typeof(ISoftDeleteable).IsAssignableFrom(type.ClrType))
                {
                    filter = AddFilter(filter, ReplacingExpressionVisitor.Replace(filterSoftDeleteable.Parameters.First(), param, filterSoftDeleteable.Body));
                }

                if (typeof(IOwnable).IsAssignableFrom(type.ClrType))
                {
                    filter = AddFilter(filter, ReplacingExpressionVisitor.Replace(filterOwnable.Parameters.First(), param, filterOwnable.Body));
                }

                if (filter != null)
                {
                    type.SetQueryFilter(Expression.Lambda(filter, param));
                }
            }
        }

        private Expression AddFilter(Expression filter, Expression newFilter)
        {
            if (filter == null)
            {
                filter = newFilter;
            }
            else
            {
                filter = Expression.And(filter, newFilter);
            }
            return filter;
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

            if (typeof(IOwnable).IsAssignableFrom(typeof(T)))
            {
                entity.Property("Owner")
                    .IsRequired();
            }
        }

        public override int SaveChanges()
        {
            SoftDelete();
            TimeTrack();
            Ownable();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            SoftDelete();
            TimeTrack();
            Ownable();
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
                    if (item.State == EntityState.Added && entity.CreationDateTime == DateTime.MinValue)
                    {
                        entity.CreationDateTime = now;
                    }
                }
            }
        }

        private void Ownable()
        {
            ChangeTracker.DetectChanges();
            var markedEntries = ChangeTracker.Entries();
            foreach (var item in markedEntries)
            {
                if (item.Entity is IOwnable entity)
                {
                    if (item.State == EntityState.Added)
                    {
                        if (Owner != Guid.Empty)
                        {
                            entity.Owner = Owner;
                        }
                        else
                        {
                            throw new Exception("Unauthorized database insert detected");
                        }
                    }
                    if (entity.Owner != Owner && Owner != Guid.Empty)
                    {
                        throw new Exception("Unauthorized database request detected");
                    }
                }
            }
        }

    }
}