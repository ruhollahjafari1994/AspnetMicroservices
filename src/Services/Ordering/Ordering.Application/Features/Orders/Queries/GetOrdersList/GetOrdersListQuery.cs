using MediatR;
using System;
using System.Collections.Generic;

namespace Ordering.Application.Features.Orders.Queries.GetOrdersList
{
    /// <summary>
    /// Defines the GetOrdersListQuery type.
    /// </summary>
    public class GetOrdersListQuery : IRequest<List<OrdersVm>>
    {
        /// <summary>
        /// Gets or sets the UserName value.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Describes the GetOrdersListQuery operation.
        /// </summary>
        public GetOrdersListQuery(string userName)
        {
            UserName = userName ?? throw new ArgumentNullException(nameof(userName));
        }
    }
}
