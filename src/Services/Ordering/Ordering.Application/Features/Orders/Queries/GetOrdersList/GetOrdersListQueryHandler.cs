using MediatR;
using Ordering.Application.Contracts.Persistence;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ordering.Application.Features.Orders.Queries.GetOrdersList
{
    /// <summary>
    /// Defines the GetOrdersListQueryHandler type.
    /// </summary>
    public class GetOrdersListQueryHandler : IRequestHandler<GetOrdersListQuery, List<OrdersVm>>
    {
        private readonly IOrderRepository _orderRepository;
        /// <summary>
        /// Describes the GetOrdersListQueryHandler operation.
        /// </summary>
        public GetOrdersListQueryHandler(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        }

        /// <summary>
        /// Describes the Handle operation.
        /// </summary>
        public async Task<List<OrdersVm>> Handle(GetOrdersListQuery request, CancellationToken cancellationToken)
        {
            var orderList = await _orderRepository.GetOrdersByUserName(request.UserName);
            var orders = new List<OrdersVm>();
            foreach (var order in orderList)
            {
                orders.Add(new OrdersVm
                {
                    Id = order.Id,
                    UserName = order.UserName,
                    TotalPrice = order.TotalPrice,
                    FirstName = order.FirstName,
                    LastName = order.LastName,
                    EmailAddress = order.EmailAddress,
                    AddressLine = order.AddressLine,
                    Country = order.Country,
                    State = order.State,
                    ZipCode = order.ZipCode,
                    PaymentIntentId = order.PaymentIntentId,
                    PaymentMethod = order.PaymentMethod
                });
            }

            return orders;
        }
    }
}
