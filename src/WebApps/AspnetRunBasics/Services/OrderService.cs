using AspnetRunBasics.Extensions;
using AspnetRunBasics.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace AspnetRunBasics.Services
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
            var response = await _client.GetAsync($"/Order/{userName}");
            return await response.ReadContentAs<List<OrderResponseModel>>();
        }

        /// <summary>
        /// Describes the CancelOrder operation.
        /// </summary>
        public async Task CancelOrder(int orderId)
        {
            var response = await _client.DeleteAsync($"/Order/{orderId}");
            if (!response.IsSuccessStatusCode)
                throw new Exception("Something went wrong when calling api.");
        }
    }
}
