using Basket.API.Entities;
using Basket.API.Repositories;
using System;
using System.Linq;
using System.Threading.Tasks;
using Testcontainers.Redis;
using Xunit;

namespace Basket.UnitTests;

/// <summary>
/// Verifies basket reservation and compensation semantics against a real Redis server.
/// </summary>
public sealed class BasketReservationIntegrationTests : IAsyncLifetime
{
    private readonly RedisContainer _redis = new RedisBuilder("redis:7-alpine").Build();
    private StackExchange.Redis.IConnectionMultiplexer _connection;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        await _redis.StartAsync();
        _connection = await StackExchange.Redis.ConnectionMultiplexer.ConnectAsync(_redis.GetConnectionString());
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        _connection?.Dispose();
        await _redis.DisposeAsync();
    }

    /// <summary>
    /// Confirms two independent repository instances cannot reserve one basket with different IDs.
    /// </summary>
    [Fact]
    public async Task ConcurrentReservations_OnlyOneCheckoutCanOwnBasket()
    {
        var firstInstance = new BasketRepository(_connection, "test");
        var secondInstance = new BasketRepository(_connection, "test");
        const string user = "concurrency-user";
        await firstInstance.UpdateBasket(new ShoppingCart(user)
        {
            Items = { new ShoppingCartItem { ProductId = "0123456789abcdef01234567", ProductName = "Phone", Quantity = 1, Price = 10m } }
        }, 0);

        var reservations = await Task.WhenAll(
            firstInstance.ReserveBasket(user, Guid.NewGuid()),
            secondInstance.ReserveBasket(user, Guid.NewGuid()));

        Assert.Single(reservations.Where(result => result.State == BasketCheckoutReservationState.Reserved));
        Assert.Single(reservations.Where(result => result.State == BasketCheckoutReservationState.Busy));
    }

    /// <summary>
    /// Confirms compensation merges the original snapshot with later edits exactly once.
    /// </summary>
    [Fact]
    public async Task RestoreCheckout_PreservesNewItemsAndRepeatedDeliveryDoesNotDuplicate()
    {
        var repository = new BasketRepository(_connection, "restore-test");
        const string user = "restore-user";
        const string originalId = "0123456789abcdef01234567";
        const string addedId = "1123456789abcdef01234567";
        await repository.UpdateBasket(new ShoppingCart(user)
        {
            Items = { new ShoppingCartItem { ProductId = originalId, ProductName = "Original", Quantity = 2, Price = 5m } }
        }, 0);
        var checkoutId = Guid.NewGuid();
        Assert.Equal(BasketCheckoutReservationState.Reserved, (await repository.ReserveBasket(user, checkoutId)).State);

        var newBasket = await repository.GetBasket(user);
        newBasket.Items.Add(new ShoppingCartItem { ProductId = addedId, ProductName = "New", Quantity = 1, Price = 7m });
        Assert.True(await repository.UpdateBasket(newBasket, newBasket.Version));

        Assert.True(await repository.RestoreCheckout(user, checkoutId));
        Assert.True(await repository.RestoreCheckout(user, checkoutId));
        var result = await repository.GetBasket(user);
        Assert.Equal(2, result.Items.Single(item => item.ProductId == originalId).Quantity);
        Assert.Equal(1, result.Items.Single(item => item.ProductId == addedId).Quantity);
    }

    /// <summary>
    /// Confirms a retry with the same idempotency key receives its original reservation.
    /// </summary>
    [Fact]
    public async Task RepeatedReservationWithSameCheckoutId_ReturnsOriginalSnapshot()
    {
        var repository = new BasketRepository(_connection, "retry-test");
        const string user = "retry-user";
        await repository.UpdateBasket(new ShoppingCart(user)
        {
            Items = { new ShoppingCartItem { ProductId = "0123456789abcdef01234567", ProductName = "Phone", Quantity = 1, Price = 10m } }
        }, 0);
        var checkoutId = Guid.NewGuid();

        var first = await repository.ReserveBasket(user, checkoutId);
        var retry = await repository.ReserveBasket(user, checkoutId);

        Assert.Equal(BasketCheckoutReservationState.Reserved, first.State);
        Assert.Equal(first.Snapshot.Items[0].ProductId, retry.Snapshot.Items[0].ProductId);
        Assert.Equal(first.Snapshot.Items[0].Price, retry.Snapshot.Items[0].Price);
    }
}
