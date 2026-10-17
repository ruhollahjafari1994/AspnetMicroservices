using Basket.API.Repositories.Interfaces;
using EventBus.Messages.Events;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Basket.API.Workflows
{
    /// <summary>
    /// Applies idempotent basket reservation transitions requested by the ordering saga.
    /// </summary>
    public sealed class BasketWorkflowHandler
    {
        /// <summary>
        /// Commits a checkout reservation after order creation.
        /// </summary>
        public async Task<BasketClearedForOrder> Handle(
            ClearBasketForOrder command,
            IBasketRepository baskets,
            ILogger<BasketWorkflowHandler> logger)
        {
            if (command.CheckoutId == Guid.Empty)
            {
                logger.LogWarning("Ignoring legacy basket clear command without a checkout ID for saga {CheckoutSagaId}",
                    command.CheckoutSagaId);
            }
            else if (!await baskets.CompleteCheckout(command.UserName, command.CheckoutId))
            {
                // A late clear after successful compensation must never remove the restored basket.
                logger.LogInformation("Checkout reservation {CheckoutId} was not active when clear arrived", command.CheckoutId);
            }

            return new BasketClearedForOrder
            {
                CheckoutId = command.CheckoutId,
                CheckoutSagaId = command.CheckoutSagaId
            };
        }

        /// <summary>
        /// Restores the saved checkout snapshot while retaining any items added since checkout began.
        /// </summary>
        public async Task<BasketRestoredForOrder> Handle(
            RestoreBasketForOrder command,
            IBasketRepository baskets,
            ILogger<BasketWorkflowHandler> logger)
        {
            if (command.CheckoutId == Guid.Empty ||
                !await baskets.RestoreCheckout(command.UserName, command.CheckoutId))
            {
                logger.LogWarning("Checkout reservation {CheckoutId} could not be restored", command.CheckoutId);
                throw new InvalidOperationException("The checkout reservation is missing or cannot be restored.");
            }

            logger.LogInformation("Restored basket for checkout {CheckoutId} and saga {CheckoutSagaId}",
                command.CheckoutId, command.CheckoutSagaId);
            return new BasketRestoredForOrder
            {
                CheckoutId = command.CheckoutId,
                CheckoutSagaId = command.CheckoutSagaId
            };
        }
    }
}
