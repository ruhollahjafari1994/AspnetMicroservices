using System;
using System.Linq;
using System.Threading.Tasks;
using AspnetRunBasics.Models;
using AspnetRunBasics.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AspnetRunBasics
{
    /// <summary>
    /// Defines the CartModel type.
    /// </summary>
    public class CartModel : PageModel
    {
        private readonly IBasketService _basketService;

        /// <summary>
        /// Describes the CartModel operation.
        /// </summary>
        public CartModel(IBasketService basketService)
        {
            _basketService = basketService ?? throw new ArgumentNullException(nameof(basketService));
        }

        /// <summary>
        /// Gets or sets the Cart value.
        /// </summary>
        public BasketModel Cart { get; set; } = new BasketModel();

        /// <summary>
        /// Describes the OnGetAsync operation.
        /// </summary>
        public async Task<IActionResult> OnGetAsync()
        {
            var userName = User.Identity!.Name!;
            Cart = await _basketService.GetBasket(userName);

            return Page();
        }

        /// <summary>
        /// Describes the OnPostRemoveToCartAsync operation.
        /// </summary>
        public async Task<IActionResult> OnPostRemoveToCartAsync(string productId)
        {
            var userName = User.Identity!.Name!;
            var basket = await _basketService.GetBasket(userName);

            var item = basket.Items.Single(x => x.ProductId == productId);
            basket.Items.Remove(item);

            var basketUpdated = await _basketService.UpdateBasket(basket);

            return RedirectToPage();
        }
    }
}
