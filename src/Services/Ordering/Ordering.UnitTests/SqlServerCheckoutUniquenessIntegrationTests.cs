using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MediatR;
using Moq;
using EventBus.Messages.Events;
using Ordering.API.Workflows;
using Ordering.Application.Features.Orders.Commands.CheckoutOrder;
using Ordering.Domain.Entities;
using Ordering.Infrastructure.Persistence;
using System;
using System.Linq;
using System.Threading.Tasks;
using Testcontainers.MsSql;
using Xunit;

namespace Ordering.UnitTests;

/// <summary>
/// Verifies checkout uniqueness using the production SQL Server provider.
/// </summary>
public sealed class SqlServerCheckoutUniquenessIntegrationTests : IAsyncLifetime
{
    private readonly MsSqlContainer _sqlServer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("Test_only_Password_123!Aa")
        .Build();

    /// <inheritdoc />
    public Task InitializeAsync() => _sqlServer.StartAsync();

    /// <inheritdoc />
    public Task DisposeAsync() => _sqlServer.DisposeAsync().AsTask();

    /// <summary>
    /// Ensures concurrent inserts with an identical checkout ID leave exactly one order.
    /// </summary>
    [Fact]
    public async Task SqlServer_unique_index_rejects_duplicate_checkout_ids()
    {
        var options = new DbContextOptionsBuilder<OrderContext>().UseSqlServer(_sqlServer.GetConnectionString()).Options;
        await using (var setup = new OrderContext(options)) await setup.Database.EnsureCreatedAsync();
        var checkoutId = Guid.NewGuid();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<bool> InsertAsync()
        {
            await using var context = new OrderContext(options);
            await gate.Task;
            context.Orders.Add(new Order
            {
                CheckoutEventId = checkoutId,
                UserName = "sql-integration",
                EmailAddress = "test@example.invalid"
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
        }

        var first = InsertAsync();
        var second = InsertAsync();
        gate.SetResult();
        var results = await Task.WhenAll(first, second);
        await using var verify = new OrderContext(options);
        Assert.Single(results, inserted => inserted);
        Assert.Equal(1, await verify.Orders.CountAsync(order => order.CheckoutEventId == checkoutId));
    }

    /// <summary>
    /// Exercises the production checkout event handler against concurrent SQL Server deliveries.
    /// </summary>
    [Fact]
    public async Task CheckoutHandler_ConcurrentDuplicateDeliveryReturnsTheSameOrder()
    {
        var options = new DbContextOptionsBuilder<OrderContext>().UseSqlServer(_sqlServer.GetConnectionString()).Options;
        await using (var setup = new OrderContext(options)) await setup.Database.EnsureCreatedAsync();
        var checkoutId = Guid.NewGuid();
        var mediator = new Mock<IMediator>();
        mediator.Setup(service => service.Send(It.IsAny<CheckoutOrderCommand>(), It.IsAny<System.Threading.CancellationToken>()))
            .Returns(async (CheckoutOrderCommand command, System.Threading.CancellationToken token) =>
            {
                await using var context = new OrderContext(options);
                var order = new Order
                {
                    CheckoutEventId = command.CheckoutEventId,
                    UserName = command.UserName,
                    TotalPrice = command.TotalPrice,
                    FirstName = command.FirstName,
                    LastName = command.LastName,
                    EmailAddress = command.EmailAddress,
                    AddressLine = command.AddressLine,
                    Country = command.Country,
                    State = command.State,
                    ZipCode = command.ZipCode,
                    PaymentIntentId = command.PaymentIntentId,
                    PaymentMethod = command.PaymentMethod
                };
                context.Orders.Add(order);
                await context.SaveChangesAsync(token);
                return order.Id;
            });
        var checkoutHandler = new BasketCheckoutHandler(mediator.Object, NullLogger<BasketCheckoutHandler>.Instance);
        var checkout = new BasketCheckoutEvent(checkoutId)
        {
            UserName = "checkout-handler-integration",
            EmailAddress = "test@example.invalid",
            Items = new System.Collections.Generic.List<BasketCheckoutItem>()
        };
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<BeginCheckoutSaga> DeliverAsync()
        {
            await using var context = new OrderContext(options);
            await start.Task;
            return await checkoutHandler.Handle(checkout, context, System.Threading.CancellationToken.None);
        }

        var first = DeliverAsync();
        var second = DeliverAsync();
        start.SetResult();
        var sagaStarts = await Task.WhenAll(first, second);

        Assert.Equal(sagaStarts[0].CheckoutSagaId, sagaStarts[1].CheckoutSagaId);
        Assert.Equal(checkoutId, sagaStarts[0].CheckoutId);
        await using var verify = new OrderContext(options);
        Assert.Equal(1, await verify.Orders.CountAsync(order => order.CheckoutEventId == checkoutId));
    }

    /// <summary>
    /// Ensures persisted audit timestamps are generated with UTC values.
    /// </summary>
    [Fact]
    public async Task OrderContext_AuditTimestampsUseUtc()
    {
        var options = new DbContextOptionsBuilder<OrderContext>().UseSqlServer(_sqlServer.GetConnectionString()).Options;
        await using (var setup = new OrderContext(options)) await setup.Database.EnsureCreatedAsync();
        await using var context = new OrderContext(options);
        var order = new Order { UserName = "audit-test", EmailAddress = "test@example.invalid" };
        context.Orders.Add(order);
        await context.SaveChangesAsync();
        Assert.Equal(DateTimeKind.Utc, order.CreatedDate.Kind);

        order.UserName = "audit-test-updated";
        await context.SaveChangesAsync();
        Assert.Equal(DateTimeKind.Utc, order.LastModifiedDate.Value.Kind);
    }
}
