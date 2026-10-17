using Microsoft.Extensions.Logging;
using Ordering.Domain.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ordering.Infrastructure.Persistence
{
    /// <summary>
    /// Defines the OrderContextSeed type.
    /// </summary>
    public class OrderContextSeed
    {
        /// <summary>
        /// Describes the SeedAsync operation.
        /// </summary>
        public static async Task SeedAsync(OrderContext orderContext, ILogger<OrderContextSeed> logger)
        {            
            if (!orderContext.Orders.Any())
            {
                orderContext.Orders.AddRange(GetPreconfiguredOrders());
                await orderContext.SaveChangesAsync();
                logger.LogInformation("Seed database associated with context {DbContextName}", typeof(OrderContext).Name);
            }
        }

        /// <summary>
        /// Describes the GetPreconfiguredOrders operation.
        /// </summary>
        private static IEnumerable<Order> GetPreconfiguredOrders()
        {
            return new List<Order>
            {
                new Order() {UserName = "demo-customer", FirstName = "Mehmet", LastName = "Ozkaya", EmailAddress = "ezozkme@gmail.com", AddressLine = "Bahcelievler", Country = "Turkey", TotalPrice = 350 }
            };
        }
    }
}
