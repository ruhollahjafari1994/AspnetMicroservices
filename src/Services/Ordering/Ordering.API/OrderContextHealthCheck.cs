using Microsoft.Extensions.Diagnostics.HealthChecks;
using Ordering.Infrastructure.Persistence;
using System.Threading;
using System.Threading.Tasks;

namespace Ordering.API
{
    /// <summary>
    /// Defines the OrderContextHealthCheck type.
    /// </summary>
    public class OrderContextHealthCheck : IHealthCheck
    {
        private readonly OrderContext _context;

        /// <summary>
        /// Executes the OrderContextHealthCheck operation.
        /// </summary>
        public OrderContextHealthCheck(OrderContext context) => _context = context;

        /// <summary>
        /// Describes the CheckHealthAsync operation.
        /// </summary>
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("Order database is reachable")
                : HealthCheckResult.Unhealthy("Order database is unreachable");
        }
    }
}
