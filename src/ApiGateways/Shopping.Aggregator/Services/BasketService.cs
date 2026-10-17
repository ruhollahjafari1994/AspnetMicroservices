using Shopping.Aggregator.Extensions;
using Shopping.Aggregator.Models;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Shopping.Aggregator.Services
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
            var response = await _client.GetAsync($"/api/v1/Basket/{userName}");
            return await response.ReadContentAs<BasketModel>();
        }        
    }
}
