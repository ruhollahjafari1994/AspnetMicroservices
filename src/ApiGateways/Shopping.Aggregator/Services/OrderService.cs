using Shopping.Aggregator.Extensions;
using Shopping.Aggregator.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Shopping.Aggregator.Services
{
    /// <summary>
    /// Defines the OrderService type.
    /// </summary>
    public class OrderService : IOrderService
    {
        private readonly HttpClient _client;

        /// <summary>
        /// Describes the OrderService operation.
        /// </summary>
        public OrderService(HttpClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <summary>
        /// Describes the GetOrdersByUserName operation.
        /// </summary>
        public async Task<IEnumerable<OrderResponseModel>> GetOrdersByUserName(string userName)
        {
            var response = await _client.GetAsync($"/api/v1/Order/{userName}");
            return await response.ReadContentAs<List<OrderResponseModel>>();
        }
    }
}
