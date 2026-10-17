using Microsoft.EntityFrameworkCore;
using Ordering.Domain.Common;
using Ordering.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ordering.Infrastructure.Persistence
{
    /// <summary>
    /// Defines the OrderContext type.
    /// </summary>
    public class OrderContext : DbContext
    {
        /// <summary>
        /// Executes the OrderContext operation.
        /// </summary>
        public OrderContext(DbContextOptions<OrderContext> options) : base(options)
        {
        }

        /// <summary>
        /// Gets or sets the Orders value.
        /// </summary>
        public DbSet<Order> Orders { get; set; }

        /// <summary>
        /// Describes the OnModelCreating operation.
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Order>()
                .HasIndex(order => order.CheckoutEventId)
                .IsUnique()
                .HasFilter("[CheckoutEventId] IS NOT NULL");

            base.OnModelCreating(modelBuilder);
        }

        /// <summary>
        /// Describes the SaveChangesAsync operation.
        /// </summary>
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            foreach (var entry in ChangeTracker.Entries<EntityBase>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedDate = DateTime.Now;
                        entry.Entity.CreatedBy = "system";
                        break;
                    case EntityState.Modified:
                        entry.Entity.LastModifiedDate = DateTime.Now;
                        entry.Entity.LastModifiedBy = "system";
                        break;
                }
            }
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
