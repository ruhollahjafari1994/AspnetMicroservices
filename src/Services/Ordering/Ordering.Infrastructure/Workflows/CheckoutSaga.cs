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
    /// <summary>
    /// Defines the CheckoutSaga type.
    /// </summary>
    public class CheckoutSaga : Saga, IRevisioned
    {
        /// <summary>
        /// Gets or sets the Id value.
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// Gets or sets the idempotency ID used to reserve and compensate the basket.
        /// </summary>
        public Guid CheckoutId { get; set; }
        /// <summary>
        /// Gets or sets the UserName value.
        /// </summary>
        public string UserName { get; set; }
        /// <summary>
        /// Gets or sets the Status value.
        /// </summary>
        public string Status { get; set; }
        /// <summary>
        /// Gets or sets the ItemsJson value.
        /// </summary>
        public string ItemsJson { get; set; } = "[]";

        /// <summary>
        /// Documents this public member.
        /// </summary>
        [NotMapped]
        public List<BasketCheckoutItem> Items
        {
            get => JsonSerializer.Deserialize<List<BasketCheckoutItem>>(ItemsJson) ?? new List<BasketCheckoutItem>();
            set => ItemsJson = JsonSerializer.Serialize(value ?? new List<BasketCheckoutItem>());
        }

        /// <summary>
        /// Describes the Configure operation.
        /// </summary>
        public static void Configure(HandlerChain chain)
        {
            chain.OnException<SagaConcurrencyException>()
                .RetryWithCooldown(
                    TimeSpan.FromMilliseconds(100),
                    TimeSpan.FromMilliseconds(250),
                    TimeSpan.FromMilliseconds(500));
        }

        /// <summary>
        /// Describes the static operation.
        /// </summary>
        public static (CheckoutSaga, ClearBasketForOrder) Start(BeginCheckoutSaga message)
        {
            var sagaId = message.CheckoutSagaId != 0 ? message.CheckoutSagaId : message.Id;
            var saga = new CheckoutSaga
            {
                Id = sagaId,
                CheckoutId = message.CheckoutId,
                UserName = message.UserName,
                Items = message.Items ?? new List<BasketCheckoutItem>(),
                Status = "ClearingBasket"
            };

            return (saga, new ClearBasketForOrder
            {
                CheckoutId = saga.CheckoutId,
                CheckoutSagaId = saga.Id,
                UserName = saga.UserName
            });
        }

        /// <summary>
        /// Describes the Handle operation.
        /// </summary>
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

        /// <summary>
        /// Describes the Handle operation.
        /// </summary>
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

            // Repeating restore is safe because Basket API tracks checkout completion atomically.
            Status = "RestoringBasket";
            return CreateRestoreMessage();
        }

        /// <summary>
        /// Describes the Handle operation.
        /// </summary>
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

        /// <summary>
        /// Describes the NotFound operation.
        /// </summary>
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

        /// <summary>
        /// Describes the CreateRestoreMessage operation.
        /// </summary>
        private RestoreBasketForOrder CreateRestoreMessage() => new RestoreBasketForOrder
        {
            CheckoutId = CheckoutId,
            CheckoutSagaId = Id,
            UserName = UserName,
            Items = Items
        };
    }
}
