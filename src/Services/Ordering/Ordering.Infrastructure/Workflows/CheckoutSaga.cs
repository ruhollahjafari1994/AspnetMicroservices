using EventBus.Messages.Events;
using Microsoft.Extensions.Logging;
using Ordering.Application.Contracts.Persistence;
using Ordering.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wolverine;
using Wolverine.Runtime.Handlers;
using Wolverine.ErrorHandling;
using JasperFx;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Ordering.API.Workflows
{
    public class CheckoutSaga : Saga, IRevisioned
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Status { get; set; }
        public string ItemsJson { get; set; } = "[]";

        [NotMapped]
        public List<BasketCheckoutItem> Items
        {
            get => JsonSerializer.Deserialize<List<BasketCheckoutItem>>(ItemsJson) ?? new List<BasketCheckoutItem>();
            set => ItemsJson = JsonSerializer.Serialize(value ?? new List<BasketCheckoutItem>());
        }

        public static void Configure(HandlerChain chain)
        {
            chain.OnException<SagaConcurrencyException>()
                .RetryWithCooldown(
                    TimeSpan.FromMilliseconds(100),
                    TimeSpan.FromMilliseconds(250),
                    TimeSpan.FromMilliseconds(500));
        }

        public static (CheckoutSaga, ClearBasketForOrder) Start(BeginCheckoutSaga message)
        {
            var saga = new CheckoutSaga
            {
                Id = message.Id,
                UserName = message.UserName,
                Items = message.Items ?? new List<BasketCheckoutItem>(),
                Status = "ClearingBasket"
            };

            return (saga, new ClearBasketForOrder
            {
                CheckoutSagaId = saga.Id,
                UserName = saga.UserName
            });
        }

        // A repeated checkout event can emit the same saga start again. Once the
        // saga exists, the duplicate start must not clear the basket a second time.
        public void Handle(BeginCheckoutSaga message)
        {
        }

        public RestoreBasketForOrder Handle(BasketClearedForOrder message)
        {
            if (Status == "CancelRequested")
            {
                Status = "RestoringBasket";
                return CreateRestoreMessage();
            }

            if (Status == "ClearingBasket")
                Status = "Confirmed";
            return null;
        }

        public RestoreBasketForOrder Handle(CancelOrderSaga message)
        {
            if (Status == "Cancelled" || Status == "CancelRequested")
                return null;

            if (Status == "ClearingBasket")
            {
                Status = "CancelRequested";
                return null;
            }

            if (Status != "Confirmed" && Status != "RestoringBasket")
                return null;

            // Repeating restore is safe because Basket.API overwrites the same Redis basket.
            Status = "RestoringBasket";
            return CreateRestoreMessage();
        }

        public async Task Handle(BasketRestoredForOrder message, IOrderRepository orders, ILogger<CheckoutSaga> logger)
        {
            if (Status != "RestoringBasket")
                return;

            var order = await orders.GetByIdAsync(Id);
            if (order != null)
                await orders.DeleteAsync(order);

            Status = "Cancelled";
            logger.LogInformation("Order {OrderId} was compensated and its basket restored", Id);
        }

        public static async Task NotFound(
            CancelOrderSaga message,
            IOrderRepository orders,
            IMessageBus bus,
            ILogger<CheckoutSaga> logger)
        {
            var order = await orders.GetByIdAsync(message.CheckoutSagaId);
            if (order == null)
                return;

            // The order and saga start message are committed together, but a
            // cancellation can reach the saga queue before its start message.
            if (order.CheckoutEventId.HasValue && message.RetryCount < 15)
            {
                await bus.ScheduleAsync(new CancelOrderSaga
                {
                    CheckoutSagaId = message.CheckoutSagaId,
                    RetryCount = message.RetryCount + 1
                }, TimeSpan.FromSeconds(1));
                return;
            }

            logger.LogWarning(
                "Cancellation for order {OrderId} was not applied because no compensating saga exists",
                message.CheckoutSagaId);
        }

        private RestoreBasketForOrder CreateRestoreMessage() => new RestoreBasketForOrder
        {
            CheckoutSagaId = Id,
            UserName = UserName,
            Items = Items
        };
    }
}
