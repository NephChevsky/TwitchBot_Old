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
        public TwitchDbContext(DbContextOptions options) : base(options)
        {
        }

        public TwitchDbContext()
        {
        }

        public DbSet<Viewer> Viewers => Set<Viewer>();
        public DbSet<Command> Commands => Set<Command>();
        public DbSet<ChatMessage> Messages => Set<ChatMessage>();
        public DbSet<Uptime> Uptimes => Set<Uptime>();
        public DbSet<ChannelReward> ChannelRewards => Set<ChannelReward>();
        public DbSet<Subscription> Subscriptions => Set<Subscription>();
        public DbSet<Token> Tokens => Set<Token>();
        public DbSet<Cheer> Cheers => Set<Cheer>();
        public DbSet<SongToAdd> SongsToAdd => Set<SongToAdd>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

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

                entity.Property(e => e.IsFollower)
                    .IsRequired()
                    .HasDefaultValue(false);

                entity.Property(e => e.IsSub)
                    .IsRequired()
                    .HasDefaultValue(false);

                entity.Property(e => e.IsMod)
                    .IsRequired()
                    .HasDefaultValue(false);

                entity.Property(e => e.IsVIP)
                    .IsRequired()
                    .HasDefaultValue(false);

                entity.Property(e => e.FirstFollowDateTime);

                AddGenericFields<Viewer>(entity);
            });
            modelBuilder.Entity<Viewer>().HasIndex(t => new { t.Id }).IsUnique(true);

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

            modelBuilder.Entity<ChannelReward>(entity =>
            {
                entity.Property(e => e.TwitchId)
                    .HasMaxLength(512);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(512);

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(512);

                entity.Property(e => e.IsEnabled)
                    .IsRequired()
                    .HasDefaultValue(false);

                entity.Property(e => e.BackgroundColor)
                    .IsRequired()
                    .HasMaxLength(7)
                    .HasDefaultValue("#ffffff");

                entity.Property(e => e.UserText)
                    .HasDefaultValue(false);
                
                entity.Property(e => e.BeginCost)
                    .IsRequired()
                    .HasDefaultValue(100);

                entity.Property(e => e.CurrentCost)
                    .IsRequired()
                    .HasDefaultValue(100);

                entity.Property(e => e.CostIncreaseAmount)
                    .IsRequired()
                    .HasDefaultValue(100);

                entity.Property(e => e.CostDecreaseTimer)
                    .IsRequired()
                    .HasDefaultValue(600);

                entity.Property(e => e.SkipRewardQueue)
                    .HasDefaultValue(false);

                entity.Property(e => e.RedemptionCooldownTime)
                    .IsRequired()
                    .HasDefaultValue(60);

                entity.Property(e => e.RedemptionPerStream)
                    .IsRequired()
                    .HasDefaultValue(0);

                entity.Property(e => e.RedemptionPerUserPerStream)
                    .IsRequired()
                    .HasDefaultValue(0);

                entity.Property(e => e.TriggerType)
                    .IsRequired();

                entity.Property(e => e.TriggerValue)
                    .IsRequired();

                entity.Property(e => e.LastUsedDateTime)
                    .IsRequired();

                AddGenericFields<ChannelReward>(entity);
            });
            modelBuilder.Entity<ChannelReward>().HasIndex(t => new { t.Id }).IsUnique(true);

            modelBuilder.Entity<Subscription>(entity =>
            {
                entity.Property(e => e.IsGift)
                    .IsRequired();

                entity.Property(e => e.GifterId)
                    .HasMaxLength(512);

                entity.Property(e => e.Tier)
                    .HasMaxLength(10);

                entity.Property(e => e.EndDateTime)
                    .IsRequired();

                AddGenericFields<Subscription>(entity);
            });
            modelBuilder.Entity<Subscription>().HasIndex(t => new { t.Id }).IsUnique(true);

            modelBuilder.Entity<Token>(entity =>
            {
                entity.Property(e => e.Name)
                    .HasMaxLength(512)
                    .IsRequired();

                entity.Property(e => e.Value)
                    .HasMaxLength(512)
                    .IsRequired();

                AddGenericFields<Token>(entity);
            });
            modelBuilder.Entity<Token>().HasIndex(t => new { t.Id }).IsUnique(true);

            modelBuilder.Entity<Cheer>(entity =>
            {
                entity.Property(e => e.Amount)
                    .IsRequired();

                AddGenericFields<Cheer>(entity);
            });
            modelBuilder.Entity<Cheer>().HasIndex(t => new { t.Id }).IsUnique(true);

            modelBuilder.Entity<SongToAdd>(entity =>
            {
                entity.Property(e => e.Uri)
                    .HasMaxLength(512)
                    .IsRequired();

                entity.Property(e => e.RewardId)
                    .HasMaxLength(512);

                entity.Property(e => e.EventId)
                    .HasMaxLength(512);

                AddGenericFields<SongToAdd>(entity);
            });
            modelBuilder.Entity<SongToAdd>().HasIndex(t => new { t.Id }).IsUnique(true);

            Expression<Func<ISoftDeleteable, bool>> filterSoftDeleteable = bm => !bm.Deleted;
            foreach (var type in modelBuilder.Model.GetEntityTypes())
            {
                Expression filter = null;
                var param = Expression.Parameter(type.ClrType, "entity");
                if (typeof(ISoftDeleteable).IsAssignableFrom(type.ClrType))
                {
                    filter = AddFilter(filter, ReplacingExpressionVisitor.Replace(filterSoftDeleteable.Parameters.First(), param, filterSoftDeleteable.Body));
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
                  .HasMaxLength(512)
                  .IsRequired();

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
                    .HasMaxLength(512)
                    .IsRequired();
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
            DateTime now = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"));
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
    }
}