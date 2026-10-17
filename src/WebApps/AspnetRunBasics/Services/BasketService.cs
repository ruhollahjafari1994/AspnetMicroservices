using AspnetRunBasics.Extensions;
using AspnetRunBasics.Models;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace AspnetRunBasics.Services
{
    /// <summary>
    /// Defines the BasketService type.
    /// </summary>
    public class BasketService : IBasketService
    {
        private readonly HttpClient _client;

        /// <summary>
        /// Describes the BasketService operation.
        /// </summary>
        public BasketService(HttpClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <summary>
        /// Describes the GetBasket operation.
        /// </summary>
        public async Task<BasketModel> GetBasket(string userName)
        {
            var response = await _client.GetAsync($"/Basket/{userName}");
            return await response.ReadContentAs<BasketModel>();
        }

        /// <summary>
        /// Describes the UpdateBasket operation.
        /// </summary>
        public async Task<BasketModel> UpdateBasket(BasketModel model)
        {
            var response = await _client.PostAsJson($"/Basket", model);
            if (response.IsSuccessStatusCode)
                return await response.ReadContentAs<BasketModel>();
            else
            {
                throw new Exception("Something went wrong when calling api.");
            }
        }

        /// <summary>
        /// Describes the CheckoutBasket operation.
        /// </summary>
        public async Task CheckoutBasket(BasketCheckoutModel model)
        {
            var response = await _client.PostAsJson($"/Basket/Checkout", model);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Something went wrong when calling api.");
            }
        }
    }
}
