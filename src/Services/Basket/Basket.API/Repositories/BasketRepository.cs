using Basket.API.Entities;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Basket.API.Repositories
{
    
        public class BasketRepository : IBasketRepository
        {
            private readonly IDistributedCache _redisCache;

            public BasketRepository(IDistributedCache cache)
            {
                _redisCache = cache ?? throw new ArgumentNullException(nameof(cache));
            }

            public async Task<ShoppingCard> GetBasket(string userName)
            {
                var basket = await _redisCache.GetStringAsync(userName);

                if (String.IsNullOrEmpty(basket))
                    return null;

                return JsonConvert.DeserializeObject<ShoppingCard>(basket);
            }

            public async Task<ShoppingCard> UpdateBasket(ShoppingCard basket)
            {
                await _redisCache.SetStringAsync(basket.UserName, JsonConvert.SerializeObject(basket));

                return await GetBasket(basket.UserName);
            }

            public async Task DeleteBasket(string userName)
            {
                await _redisCache.RemoveAsync(userName);
            }
        }
    }
 
