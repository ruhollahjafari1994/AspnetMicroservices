using Microsoft.Extensions.Diagnostics.HealthChecks;
using Ordering.Infrastructure.Persistence;
using System.Threading;
using System.Threading.Tasks;

namespace Ordering.API
{
    public class OrderContextHealthCheck : IHealthCheck
    {
        private readonly OrderContext _context;

        public OrderContextHealthCheck(OrderContext context) => _context = context;

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
