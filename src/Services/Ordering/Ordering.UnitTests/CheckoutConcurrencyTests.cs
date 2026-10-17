using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Ordering.Domain.Entities;
using Ordering.Infrastructure.Persistence;
using Ordering.API.Workflows;
using Xunit;

namespace Ordering.UnitTests
{
    /// <summary>
    /// Defines the CheckoutConcurrencyTests type.
    /// </summary>
    public class CheckoutConcurrencyTests
    {
        /// <summary>
        /// Ensures audit timestamps assigned by the context use UTC for inserts and updates.
        /// </summary>
        [Fact]
        public async Task Audit_timestamps_are_utc()
        {
            var connectionString = $"Data Source=audit-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
            await using var keepAlive = new SqliteConnection(connectionString);
            await keepAlive.OpenAsync();
            var options = new DbContextOptionsBuilder<OrderContext>().UseSqlite(connectionString).Options;
            await using (var setup = new SqliteOrderContext(options)) await setup.Database.EnsureCreatedAsync();

            await using var context = new SqliteOrderContext(options);
            var order = new Order { UserName = "audit-test", EmailAddress = "test@example.invalid" };
            context.Orders.Add(order);
            await context.SaveChangesAsync();
            Assert.Equal(DateTimeKind.Utc, order.CreatedDate.Kind);

            order.UserName = "audit-test-updated";
            await context.SaveChangesAsync();
            Assert.Equal(DateTimeKind.Utc, order.LastModifiedDate.Value.Kind);
        }

        /// <summary>
        /// Describes the Concurrent_inserts_with_same_checkout_id_create_only_one_order operation.
        /// </summary>
        [Fact]
        public async Task Concurrent_inserts_with_same_checkout_id_create_only_one_order()
        {
            var connectionString = $"Data Source=checkout-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
            await using var keepAlive = new SqliteConnection(connectionString);
            await keepAlive.OpenAsync();

            var options = new DbContextOptionsBuilder<OrderContext>()
                .UseSqlite(connectionString)
                .Options;

            await using (var setup = new SqliteOrderContext(options))
                await setup.Database.EnsureCreatedAsync();

            var checkoutId = Guid.NewGuid();
            var start = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            async Task<bool> TryInsertAsync()
            {
                await using var context = new SqliteOrderContext(options);
                await start.Task;
                context.Orders.Add(new Order
                {
                    CheckoutEventId = checkoutId,
                    UserName = "concurrency-test",
                    TotalPrice = 10m,
                    FirstName = "Test",
                    LastName = "User",
                    EmailAddress = "test@example.invalid",
                    AddressLine = "Test address",
                    Country = "Test",
                    State = "Test",
                    ZipCode = "00000",
                    PaymentIntentId = "pi_test_placeholder",
                    PaymentMethod = 1
                });

                try
                {
                    await context.SaveChangesAsync();
                    return true;
                }
                catch (DbUpdateException)
                {
                    return false;
                }
                catch (SqliteException)
                {
                    // SQLite may report a write lock when both writers arrive together.
                    // The invariant under test is still that a single order survives.
                    return false;
                }
            }

            var first = TryInsertAsync();
            var second = TryInsertAsync();
            start.SetResult(true);
            var outcomes = await Task.WhenAll(first, second);

            await using var verify = new SqliteOrderContext(options);
            Assert.Equal(1, outcomes.Count(inserted => inserted));
            Assert.Equal(1, await verify.Orders.CountAsync(order => order.CheckoutEventId == checkoutId));
        }

        /// <summary>
        /// Defines the SqliteOrderContext type.
        /// </summary>
        private sealed class SqliteOrderContext : OrderContext
        {
            /// <summary>
            /// Executes the SqliteOrderContext operation.
            /// </summary>
            public SqliteOrderContext(DbContextOptions<OrderContext> options) : base(options)
            {
            }

            /// <summary>
            /// Describes the OnModelCreating operation.
            /// </summary>
            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);
                modelBuilder.Entity<CheckoutSaga>()
                    .Property(saga => saga.ItemsJson)
                    .HasColumnType("TEXT");
            }
        }
    }
}
