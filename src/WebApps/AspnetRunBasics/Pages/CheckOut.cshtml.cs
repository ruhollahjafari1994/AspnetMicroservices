using System;
using System.Threading.Tasks;
using AspnetRunBasics.Models;
using AspnetRunBasics.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AspnetRunBasics
{
    /// <summary>
    /// Defines the CheckOutModel type.
    /// </summary>
    public class CheckOutModel : PageModel
    {
        private readonly IBasketService _basketService;
        private readonly IOrderService _orderService;

        /// <summary>
        /// Describes the CheckOutModel operation.
        /// </summary>
        public CheckOutModel(IBasketService basketService, IOrderService orderService)
        {
            _basketService = basketService ?? throw new ArgumentNullException(nameof(basketService));
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
        }

        /// <summary>
        /// Gets or sets the Order value.
        /// </summary>
        [BindProperty]
        public BasketCheckoutModel Order { get; set; }

        /// <summary>
        /// Gets or sets the Cart value.
        /// </summary>
        public BasketModel Cart { get; set; } = new BasketModel();

        /// <summary>
        /// Describes the OnGetAsync operation.
        /// </summary>
        public async Task<IActionResult> OnGetAsync()
        {
            if (Order == null)
            {
                Order = new BasketCheckoutModel { CheckoutId = Guid.NewGuid() };
            }

            var userName = User.Identity!.Name!;
            Cart = await _basketService.GetBasket(userName);

            return Page();
        }

        /// <summary>
        /// Describes the OnPostCheckOutAsync operation.
        /// </summary>
        public async Task<IActionResult> OnPostCheckOutAsync()
        {
            var userName = User.Identity!.Name!;
            Cart = await _basketService.GetBasket(userName);

            if (!ModelState.IsValid)
            {
                return Page();
            }

            Order.UserName = userName;
            Order.TotalPrice = Cart.TotalPrice;

            await _basketService.CheckoutBasket(Order);

            return RedirectToPage("Confirmation", "OrderSubmitted");
        }
    }
}
