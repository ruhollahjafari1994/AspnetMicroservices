using Basket.API.Entities;
using Basket.API.GrpcServices;
using Basket.API.Repositories.Interfaces;
using EventBus.Messages.Events;
using EventBus.Messages.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Wolverine;

namespace Basket.API.Controllers
{
    /// <summary>
    /// Defines the BasketController type.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/v1/[controller]")]
    public class BasketController : ControllerBase
    {
        private readonly IBasketRepository _repository;
        private readonly DiscountGrpcService _discountGrpcService;
        private readonly IMessageBus _messageBus;

        /// <summary>
        /// Describes the BasketController operation.
        /// </summary>
        public BasketController(IBasketRepository repository, DiscountGrpcService discountGrpcService, IMessageBus messageBus)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _discountGrpcService = discountGrpcService ?? throw new ArgumentNullException(nameof(discountGrpcService));
            _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
        }
        
        /// <summary>
        /// Describes the GetBasket operation.
        /// </summary>
        [HttpGet("{userName}", Name = "GetBasket")]
        [ProducesResponseType(typeof(ShoppingCart), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<ShoppingCart>> GetBasket(string userName)
        {
            if (!IsOwner(userName)) return Forbid();
            var basket = await _repository.GetBasket(userName);
            return Ok(basket ?? new ShoppingCart(userName));
        }

        /// <summary>
        /// Describes the UpdateBasket operation.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ShoppingCart), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<ShoppingCart>> UpdateBasket([FromBody] ShoppingCart basket)
        {
            if (basket == null || !IsOwner(basket.UserName)) return Forbid();
            // Communicate with Discount.Grpc and calculate lastest prices of products into sc
            foreach (var item in basket.Items)
            {
                var coupon = await _discountGrpcService.GetDiscount(item.ProductName);
                item.Price -= coupon.Amount;
            }

            return Ok(await _repository.UpdateBasket(basket));
        }

        /// <summary>
        /// Describes the DeleteBasket operation.
        /// </summary>
        [HttpDelete("{userName}", Name = "DeleteBasket")]        
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> DeleteBasket(string userName)
        {
            if (!IsOwner(userName)) return Forbid();
            await _repository.DeleteBasket(userName);
            return Ok();
        }

        /// <summary>
        /// Describes the Checkout operation.
        /// </summary>
        [Route("[action]")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Checkout([FromBody] BasketCheckout basketCheckout)
        {
            if (basketCheckout == null || !IsOwner(basketCheckout.UserName)) return Forbid();
            // Cash on delivery is the only enabled option. Never accept a client-supplied
            // card token until a real payment provider can verify it server side.
            if (basketCheckout.PaymentMethod != 0)
                return BadRequest(new { error = "Only cash on delivery is currently available." });

            var basket = await _repository.GetBasket(basketCheckout.UserName);
            if (basket == null || basket.Items == null || basket.Items.Count == 0)
                return BadRequest(new { error = "The basket is empty." });

            var eventMessage = new BasketCheckoutEvent(basketCheckout.CheckoutId ?? Guid.NewGuid())
            {
                UserName = basketCheckout.UserName,
                TotalPrice = basket.TotalPrice,
                FirstName = basketCheckout.FirstName,
                LastName = basketCheckout.LastName,
                EmailAddress = basketCheckout.EmailAddress,
                AddressLine = basketCheckout.AddressLine,
                Country = basketCheckout.Country,
                State = basketCheckout.State,
                ZipCode = basketCheckout.ZipCode,
                PaymentMethod = basketCheckout.PaymentMethod,
                Items = basket.Items.Select(item => new BasketCheckoutItem
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    Color = item.Color,
                    Price = item.Price
                }).ToList()
            };

            await _messageBus.SendAsync(eventMessage);
            return Accepted(new { checkoutId = eventMessage.Id, status = "Processing" });
        }

        /// <summary>
        /// Describes the IsOwner operation.
        /// </summary>
        private bool IsOwner(string userName) => string.Equals(User.Identity?.Name, userName, StringComparison.OrdinalIgnoreCase);
    }
}
