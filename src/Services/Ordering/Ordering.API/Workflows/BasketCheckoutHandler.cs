using EventBus.Messages.Events;
using MediatR;
using Ordering.Application.Features.Orders.Commands.CheckoutOrder;
using Ordering.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;
using System;
using Polly;
using System.Threading;

namespace Ordering.API.Workflows
{
    /// <summary>
    /// Defines the BasketCheckoutHandler type.
    /// </summary>
    public class BasketCheckoutHandler
    {
        private readonly IMediator _mediator;
        private readonly ILogger<BasketCheckoutHandler> _logger;

        /// <summary>
        /// Describes the BasketCheckoutHandler operation.
        /// </summary>
        public BasketCheckoutHandler(IMediator mediator, ILogger<BasketCheckoutHandler> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Describes the Handle operation.
        /// </summary>
        public async Task<BeginCheckoutSaga> Handle(BasketCheckoutEvent checkout, OrderContext orderContext, CancellationToken cancellationToken)
        {
            var retryPolicy = Policy
                .Handle<Exception>(IsTransientSqlFailure)
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, retryAttempt - 1)),
                    onRetry: (exception, delay, retryCount, context) =>
                    {
                        _logger.LogWarning(exception,
                            "Transient SQL failure while processing checkout {CheckoutEventId}; retry {RetryCount} in {Delay}",
                            checkout.Id, retryCount, delay);
                        // A failed SaveChanges can leave stale tracked state in this scoped context.
                        orderContext.ChangeTracker.Clear();
                    });

            return await retryPolicy.ExecuteAsync(
                (_, token) => HandleOnce(checkout, orderContext, token),
                new Context(), cancellationToken);
        }

        /// <summary>
        /// Describes the HandleOnce operation.
        /// </summary>
        private async Task<BeginCheckoutSaga> HandleOnce(BasketCheckoutEvent checkout, OrderContext orderContext, CancellationToken cancellationToken)
        {
            var existingOrder = await orderContext.Orders
                .AsNoTracking()
                .SingleOrDefaultAsync(order => order.CheckoutEventId == checkout.Id, cancellationToken);

            var orderId = existingOrder?.Id;
            if (!orderId.HasValue)
            {
                try
                {
                    orderId = await _mediator.Send(new CheckoutOrderCommand
                    {
                        CheckoutEventId = checkout.Id,
                        UserName = checkout.UserName,
                        TotalPrice = checkout.TotalPrice,
                        FirstName = checkout.FirstName,
                        LastName = checkout.LastName,
                        EmailAddress = checkout.EmailAddress,
                        AddressLine = checkout.AddressLine,
                        Country = checkout.Country,
                        State = checkout.State,
                        ZipCode = checkout.ZipCode,
                        PaymentIntentId = checkout.PaymentIntentId,
                        PaymentMethod = checkout.PaymentMethod
                    }, cancellationToken);
                }
                catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
                {
                    // A concurrent delivery with the same checkout id may have
                    // inserted the order first. Let only that unique-key race pass.
                    orderContext.ChangeTracker.Clear();
                    var concurrentOrder = await orderContext.Orders
                        .AsNoTracking()
                        .SingleOrDefaultAsync(order => order.CheckoutEventId == checkout.Id, cancellationToken);
                    if (concurrentOrder == null)
                        throw;

                    orderId = concurrentOrder.Id;
                }
            }

            return new BeginCheckoutSaga
            {
                CheckoutSagaId = orderId.Value,
                Id = orderId.Value,
                UserName = checkout.UserName,
                Items = checkout.Items.Select(item => new BasketCheckoutItem
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    Color = item.Color,
                    Price = item.Price
                }).ToList()
            };
        }

        /// <summary>
        /// Describes the IsTransientSqlFailure operation.
        /// </summary>
        private static bool IsTransientSqlFailure(Exception exception)
        {
            while (exception != null)
            {
                if (exception is SqlException sqlException && sqlException.IsTransient)
                    return true;

                exception = exception.InnerException;
            }

            return false;
        }

        /// <summary>
        /// Describes the IsUniqueConstraintViolation operation.
        /// </summary>
        private static bool IsUniqueConstraintViolation(DbUpdateException exception)
        {
            for (Exception current = exception; current != null; current = current.InnerException)
            {
                if (current is SqlException sqlException &&
                    sqlException.Errors.Cast<SqlError>().Any(error => error.Number is 2601 or 2627))
                    return true;
            }

            return false;
        }
    }
}
