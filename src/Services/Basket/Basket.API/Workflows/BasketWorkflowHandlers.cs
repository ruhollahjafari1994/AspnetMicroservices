using Basket.API.Entities;
using Basket.API.Repositories.Interfaces;
using EventBus.Messages.Events;
using System.Linq;
using System.Threading.Tasks;

namespace Basket.API.Workflows
{
    public class BasketWorkflowHandlers
    {
        public async Task<BasketClearedForOrder> Handle(ClearBasketForOrder command, IBasketRepository baskets)
        {
            await baskets.DeleteBasket(command.UserName);
            return new BasketClearedForOrder { CheckoutSagaId = command.CheckoutSagaId };
        }

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
