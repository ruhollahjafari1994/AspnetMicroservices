using Basket.API.Entities;
using Basket.API.Repositories.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace Basket.API.Repositories
{
    /// <summary>
    /// Defines the BasketRepository type.
    /// </summary>
    public class BasketRepository : IBasketRepository
    {
        private readonly IDistributedCache _redisCache;

        /// <summary>
        /// Describes the BasketRepository operation.
        /// </summary>
        public BasketRepository(IDistributedCache cache)
        {
            _redisCache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        /// <summary>
        /// Describes the GetBasket operation.
        /// </summary>
        public async Task<ShoppingCart> GetBasket(string userName)
        {
            var basket = await _redisCache.GetStringAsync(userName);

            if (String.IsNullOrEmpty(basket))
                return null;            

            return JsonConvert.DeserializeObject<ShoppingCart>(basket);
        }
        
        /// <summary>
        /// Describes the UpdateBasket operation.
        /// </summary>
        public async Task<ShoppingCart> UpdateBasket(ShoppingCart basket)
        {
            await _redisCache.SetStringAsync(basket.UserName, JsonConvert.SerializeObject(basket));
            
            return await GetBasket(basket.UserName);
        }

        /// <summary>
        /// Describes the DeleteBasket operation.
        /// </summary>
        public async Task DeleteBasket(string userName)
        {
            await _redisCache.RemoveAsync(userName);
        }
    }
}
