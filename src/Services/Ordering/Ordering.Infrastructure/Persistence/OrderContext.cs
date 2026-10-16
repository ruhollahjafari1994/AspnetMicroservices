using Microsoft.EntityFrameworkCore;
using Ordering.Domain.Common;
using Ordering.Domain.Entities;
using Ordering.API.Workflows;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ordering.Infrastructure.Persistence
{
    public class OrderContext : DbContext
    {
        public OrderContext(DbContextOptions<OrderContext> options) : base(options)
        {
        }

        public DbSet<Order> Orders { get; set; }
        public DbSet<CheckoutSaga> CheckoutSagas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Order>()
                .HasIndex(order => order.CheckoutEventId)
                .IsUnique()
                .HasFilter("[CheckoutEventId] IS NOT NULL");

            modelBuilder.Entity<CheckoutSaga>(entity =>
            {
                entity.ToTable("CheckoutSagas");
                entity.HasKey(saga => saga.Id);
                entity.Property(saga => saga.Id).ValueGeneratedNever();
                entity.Property(saga => saga.UserName).HasMaxLength(256);
                entity.Property(saga => saga.Status).HasMaxLength(32);
                entity.Property(saga => saga.ItemsJson).HasColumnType("nvarchar(max)");
                entity.Property(saga => saga.Version).IsConcurrencyToken();
            });

            base.OnModelCreating(modelBuilder);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            foreach (var entry in ChangeTracker.Entries<EntityBase>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedDate = DateTime.Now;
                        entry.Entity.CreatedBy = "swn";
                        break;
                    case EntityState.Modified:
                        entry.Entity.LastModifiedDate = DateTime.Now;
                        entry.Entity.LastModifiedBy = "swn";
                        break;
                }
            }
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
