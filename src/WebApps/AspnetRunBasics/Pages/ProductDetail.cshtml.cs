using System;
using System.Threading.Tasks;
using AspnetRunBasics.Models;
using AspnetRunBasics.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AspnetRunBasics
{
    /// <summary>
    /// Defines the ProductDetailModel type.
    /// </summary>
    public class ProductDetailModel : PageModel
    {
        private readonly ICatalogService _catalogService;
        private readonly IBasketService _basketService;

        /// <summary>
        /// Describes the ProductDetailModel operation.
        /// </summary>
        public ProductDetailModel(ICatalogService catalogService, IBasketService basketService)
        {
            _catalogService = catalogService ?? throw new ArgumentNullException(nameof(catalogService));
            _basketService = basketService ?? throw new ArgumentNullException(nameof(basketService));
        }

        /// <summary>
        /// Gets or sets the Product value.
        /// </summary>
        public CatalogModel Product { get; set; }

        /// <summary>
        /// Gets or sets the Color value.
        /// </summary>
        [BindProperty]
        public string Color { get; set; }

        /// <summary>
        /// Gets or sets the Quantity value.
        /// </summary>
        [BindProperty]
        public int Quantity { get; set; }

        /// <summary>
        /// Describes the OnGetAsync operation.
        /// </summary>
        public async Task<IActionResult> OnGetAsync(string productId)
        {
            if (productId == null)
            {
                return NotFound();
            }

            Product = await _catalogService.GetCatalog(productId);
            if (Product == null)
            {
                return NotFound();
            }
            return Page();
        }

        /// <summary>
        /// Describes the OnPostAddToCartAsync operation.
        /// </summary>
        public async Task<IActionResult> OnPostAddToCartAsync(string productId)
        {
            var product = await _catalogService.GetCatalog(productId);

            var userName = User.Identity!.Name!;
            var basket = await _basketService.GetBasket(userName);

            basket.Items.Add(new BasketItemModel
            {
                ProductId = productId,
                ProductName = product.Name,
                Price = product.Price,
                Quantity = Quantity,
                Color = Color
            });

            var basketUpdated = await _basketService.UpdateBasket(basket);

            return RedirectToPage("Cart");
        }
    }
}
