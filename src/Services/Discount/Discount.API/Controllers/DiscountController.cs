using Discount.API.Entities;
using Discount.API.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Discount.API.Controllers
{
    /// <summary>
    /// Defines the DiscountController type.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/v1/[controller]")]
    public class DiscountController : ControllerBase
    {
        private readonly IDiscountRepository _repository;

        /// <summary>
        /// Describes the DiscountController operation.
        /// </summary>
        public DiscountController(IDiscountRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }
        
        /// <summary>
        /// Describes the GetDiscount operation.
        /// </summary>
        [HttpGet("{productName}", Name = "GetDiscount")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(Coupon), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<Coupon>> GetDiscount(string productName)
        {
            var discount = await _repository.GetDiscount(productName);
            return Ok(discount);
        }

        /// <summary>
        /// Describes the CreateDiscount operation.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Administrator")]
        [ProducesResponseType(typeof(Coupon), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<Coupon>> CreateDiscount([FromBody] Coupon coupon)
        {
            await _repository.CreateDiscount(coupon);
            return CreatedAtRoute("GetDiscount", new { productName = coupon.ProductName }, coupon);
        }

        /// <summary>
        /// Describes the UpdateBasket operation.
        /// </summary>
        [HttpPut]
        [Authorize(Roles = "Administrator")]
        [ProducesResponseType(typeof(Coupon), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<Coupon>> UpdateBasket([FromBody] Coupon coupon)
        {
            return Ok(await _repository.UpdateDiscount(coupon));
        }

        /// <summary>
        /// Describes the DeleteDiscount operation.
        /// </summary>
        [HttpDelete("{productName}", Name = "DeleteDiscount")]
        [Authorize(Roles = "Administrator")]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<bool>> DeleteDiscount(string productName)
        {
            return Ok(await _repository.DeleteDiscount(productName));
        }
    }
}
