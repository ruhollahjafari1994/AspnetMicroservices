using EventBus.Messages.Events;
using Ordering.API.Workflows;
using System;
using System.Collections.Generic;
using Xunit;

namespace Ordering.UnitTests;

/// <summary>
/// Exercises the checkout saga's cancellation and delayed-message transitions.
/// </summary>
public sealed class CheckoutSagaTransitionTests
{
    /// <summary>
    /// Ensures cancellation requests compensation once and a delayed clear acknowledgement cannot undo it.
    /// </summary>
    [Fact]
    public void CancellationDuringClear_EmitsRestoreAndIgnoresDelayedClearAcknowledgement()
    {
        var checkoutId = Guid.NewGuid();
        var (saga, clear) = CheckoutSaga.Start(new BeginCheckoutSaga
        {
            Id = 71,
            CheckoutSagaId = 71,
            CheckoutId = checkoutId,
            UserName = "saga-user",
            Items = new List<BasketCheckoutItem> { new() { ProductId = "product", Quantity = 1, Price = 8m } }
        });
        Assert.Equal(checkoutId, clear.CheckoutId);
        Assert.Equal("ClearingBasket", saga.Status);

        var noImmediateRestore = saga.Handle(new CancelOrderSaga { CheckoutSagaId = saga.Id });
        Assert.Null(noImmediateRestore);
        Assert.Equal("CancelRequested", saga.Status);

        var restore = saga.Handle(new BasketClearedForOrder { CheckoutSagaId = saga.Id, CheckoutId = checkoutId });
        Assert.Equal("RestoringBasket", saga.Status);
        Assert.Equal(checkoutId, restore.CheckoutId);
        Assert.Null(saga.Handle(new BasketClearedForOrder { CheckoutSagaId = saga.Id, CheckoutId = checkoutId }));
        Assert.Equal("RestoringBasket", saga.Status);
    }
}
