using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AspnetRunBasics.Models;
using AspnetRunBasics.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AspnetRunBasics
{
    /// <summary>
    /// Defines the ProductModel type.
    /// </summary>
    public class ProductModel : PageModel
    {
        private readonly ICatalogService _catalogService;
        private readonly IBasketService _basketService;

        /// <summary>
        /// Describes the ProductModel operation.
        /// </summary>
        public ProductModel(ICatalogService catalogService, IBasketService basketService)
        {
            _catalogService = catalogService ?? throw new ArgumentNullException(nameof(catalogService));
            _basketService = basketService ?? throw new ArgumentNullException(nameof(basketService));
        }

        /// <summary>
        /// Gets or sets the CategoryList value.
        /// </summary>
        public IEnumerable<string> CategoryList { get; set; } = new List<string>();
        /// <summary>
        /// Gets or sets the ProductList value.
        /// </summary>
        public IEnumerable<CatalogModel> ProductList { get; set; } = new List<CatalogModel>();


        /// <summary>
        /// Gets or sets the SelectedCategory value.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string SelectedCategory { get; set; }

        /// <summary>
        /// Gets or sets the Search value.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string Search { get; set; }

        /// <summary>
        /// Describes the OnGetAsync operation.
        /// </summary>
        public async Task<IActionResult> OnGetAsync(string categoryName)
        {
            var allProducts = await _catalogService.GetCatalog();
            CategoryList = allProducts.Select(p => p.Category).Distinct();
            var productList = string.IsNullOrWhiteSpace(Search)
                ? allProducts
                : await _catalogService.GetCatalogByName(Search.Trim());

            if (!string.IsNullOrWhiteSpace(categoryName))
            {
                ProductList = productList.Where(p => p.Category == categoryName);
                SelectedCategory = categoryName;
            }
            else
            {
                ProductList = productList;
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
                Quantity = 1,
                Color = "Black"
            });

            var basketUpdated = await _basketService.UpdateBasket(basket);

            return RedirectToPage("Cart");
        }
    }
}
