using Basket.API.Entities;
using Basket.API.Repositories.Interfaces;
using EventBus.Messages.Events;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;

namespace Basket.API.Workflows
{
    /// <summary>
    /// Defines the BasketWorkflowHandler type.
    /// </summary>
    public class BasketWorkflowHandler
    {
        /// <summary>
        /// Describes the Handle operation.
        /// </summary>
        public async Task<BasketClearedForOrder> Handle(
            ClearBasketForOrder command,
            IBasketRepository baskets,
            ILogger<BasketWorkflowHandler> logger)
        {
            logger.LogInformation("Clearing basket for checkout saga {CheckoutSagaId} and user {UserName}",
                command.CheckoutSagaId, command.UserName);
            await baskets.DeleteBasket(command.UserName);
            logger.LogInformation("Basket cleared for checkout saga {CheckoutSagaId}", command.CheckoutSagaId);
            return new BasketClearedForOrder { CheckoutSagaId = command.CheckoutSagaId };
        }

        /// <summary>
        /// Describes the Handle operation.
        /// </summary>
        public async Task<BasketRestoredForOrder> Handle(RestoreBasketForOrder command, IBasketRepository baskets)
        {
            var basket = new ShoppingCart(command.UserName)
            {
                Items = command.Items.Select(item => new ShoppingCartItem
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    Color = item.Color,
                    Price = item.Price
                }).ToList()
            };

            await baskets.UpdateBasket(basket);
            return new BasketRestoredForOrder { CheckoutSagaId = command.CheckoutSagaId };
        }
    }
}
