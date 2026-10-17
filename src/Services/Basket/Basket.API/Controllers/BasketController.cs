using Basket.API.Entities;
using Basket.API.GrpcServices;
using Basket.API.Repositories.Interfaces;
using Basket.API.Services;
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
        private readonly IProductPricingService _productPricing;
        private readonly IMessageBus _messageBus;

        /// <summary>
        /// Describes the BasketController operation.
        /// </summary>
        public BasketController(IBasketRepository repository, IProductPricingService productPricing, IMessageBus messageBus)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _productPricing = productPricing ?? throw new ArgumentNullException(nameof(productPricing));
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
            if (basket == null || string.IsNullOrWhiteSpace(basket.UserName)) return BadRequest();
            if (!IsOwner(basket.UserName)) return Forbid();
            if (basket.Items == null)
            {
                return BadRequest(new { error = "Basket items are required." });
            }

            if (basket.Items.Any(item => item == null || item.Quantity < 1 || item.Quantity > 100) ||
                basket.Items.Any(item => !IsValidProductId(item.ProductId)))
            {
                return BadRequest(new { error = "Product IDs and quantities are invalid." });
            }

            if (basket.Items.GroupBy(item => new { item.ProductId, Color = item.Color ?? string.Empty }).Any(group => group.Count() > 1))
            {
                return BadRequest(new { error = "Duplicate basket lines are not allowed." });
            }

            foreach (var item in basket.Items)
            {
                var trustedProduct = await _productPricing.GetPriceAsync(item.ProductId, HttpContext.RequestAborted);
                if (trustedProduct == null || !trustedProduct.IsAvailable)
                {
                    return UnprocessableEntity(new { error = "A requested product does not exist or is unavailable." });
                }

                item.ProductId = trustedProduct.ProductId;
                item.ProductName = trustedProduct.ProductName;
                item.Price = trustedProduct.UnitPrice;
            }

            if (!await _repository.UpdateBasket(basket, basket.Version))
            {
                return Conflict(new { error = "The basket changed. Reload it and retry." });
            }

            return Ok(await _repository.GetBasket(basket.UserName));
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
            if (basketCheckout == null || string.IsNullOrWhiteSpace(basketCheckout.UserName)) return BadRequest();
            if (!IsOwner(basketCheckout.UserName)) return Forbid();
            if (!basketCheckout.CheckoutId.HasValue || basketCheckout.CheckoutId.Value == Guid.Empty)
                return BadRequest(new { error = "A non-empty checkoutId is required and must be reused for retries." });
            // Cash on delivery is the only enabled option. Never accept a client-supplied
            // card token until a real payment provider can verify it server side.
            if (basketCheckout.PaymentMethod != 0)
                return BadRequest(new { error = "Only cash on delivery is currently available." });

            var checkoutId = basketCheckout.CheckoutId.Value;
            var priorReservation = await _repository.GetCheckoutReservation(basketCheckout.UserName, checkoutId);
            if (priorReservation?.State is BasketCheckoutReservationState.Committed or BasketCheckoutReservationState.Restored)
            {
                return Accepted(new { checkoutId, status = priorReservation.State.ToString() });
            }

            ShoppingCart basket;
            if (priorReservation?.State == BasketCheckoutReservationState.Reserved)
            {
                basket = priorReservation.Snapshot;
            }
            else
            {
                basket = await _repository.GetBasket(basketCheckout.UserName);
                if (basket.Items == null || basket.Items.Count == 0)
                    return BadRequest(new { error = "The basket is empty." });

                foreach (var item in basket.Items)
                {
                    var trustedProduct = await _productPricing.GetPriceAsync(item.ProductId, HttpContext.RequestAborted);
                    if (trustedProduct == null || !trustedProduct.IsAvailable)
                        return UnprocessableEntity(new { error = "A basket product does not exist or is unavailable." });
                    item.ProductId = trustedProduct.ProductId;
                    item.ProductName = trustedProduct.ProductName;
                    item.Price = trustedProduct.UnitPrice;
                }

                var versionBeforePricing = basket.Version;
                if (!await _repository.UpdateBasket(basket, versionBeforePricing))
                    return Conflict(new { error = "The basket changed while checkout pricing was refreshed. Retry with the same checkoutId." });

                var reservation = await _repository.ReserveBasket(basketCheckout.UserName, checkoutId);
                if (reservation.State == BasketCheckoutReservationState.Busy)
                    return Conflict(new { error = "Another checkout is already in progress for this basket." });
                if (reservation.State == BasketCheckoutReservationState.Empty || reservation.Snapshot == null)
                    return BadRequest(new { error = "The basket is empty." });

                basket = reservation.Snapshot;
                if (reservation.State is BasketCheckoutReservationState.Committed or BasketCheckoutReservationState.Restored)
                    return Accepted(new { checkoutId, status = reservation.State.ToString() });
            }

            var eventMessage = new BasketCheckoutEvent(checkoutId)
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

        /// <summary>
        /// Checks that a product identifier is a MongoDB ObjectId encoded as 24 hexadecimal characters.
        /// </summary>
        private static bool IsValidProductId(string productId) =>
            productId != null && productId.Length == 24 && productId.All(Uri.IsHexDigit);
    }
}
