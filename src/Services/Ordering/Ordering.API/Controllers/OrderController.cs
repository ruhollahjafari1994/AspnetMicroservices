using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Ordering.Application.Features.Orders.Commands.CheckoutOrder;
using Ordering.Application.Features.Orders.Commands.UpdateOrder;
using Ordering.Application.Contracts.Persistence;
using Ordering.Application.Features.Orders.Queries.GetOrdersList;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using EventBus.Messages.Events;
using EventBus.Messages.Common;
using Wolverine;

namespace Ordering.API.Controllers
{
    /// <summary>
    /// Defines the OrderController type.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/v1/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMessageBus _messageBus;
        private readonly IOrderRepository _orders;

        /// <summary>
        /// Describes the OrderController operation.
        /// </summary>
        public OrderController(IMediator mediator, IMessageBus messageBus, IOrderRepository orders)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
            _orders = orders ?? throw new ArgumentNullException(nameof(orders));
        }
        
        /// <summary>
        /// Describes the GetOrdersByUserName operation.
        /// </summary>
        [HttpGet("{userName}", Name = "GetOrder")]
        [ProducesResponseType(typeof(IEnumerable<OrdersVm>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IEnumerable<OrdersVm>>> GetOrdersByUserName(string userName)
        {
            if (!string.Equals(User.Identity?.Name, userName, StringComparison.OrdinalIgnoreCase)) return Forbid();
            var query = new GetOrdersListQuery(userName);
            var orders = await _mediator.Send(query);
            return Ok(orders);
        }

        // testing purpose
        /// <summary>
        /// Describes the CheckoutOrder operation.
        /// </summary>
        [HttpPost(Name = "CheckoutOrder")]
        [Authorize(Roles = "Administrator")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<ActionResult<int>> CheckoutOrder([FromBody] CheckoutOrderCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Describes the UpdateOrder operation.
        /// </summary>
        [HttpPut(Name = "UpdateOrder")]
        [Authorize(Roles = "Administrator")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public async Task<ActionResult> UpdateOrder([FromBody] UpdateOrderCommand command)
        {
            await _mediator.Send(command);
            return NoContent();
        }

        /// <summary>
        /// Describes the DeleteOrder operation.
        /// </summary>
        [HttpDelete("{id}", Name = "DeleteOrder")]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesDefaultResponseType]
        public async Task<ActionResult> DeleteOrder(int id)
        {
            var order = await _orders.GetByIdAsync(id);
            if (order == null)
                return NotFound();
            if (!string.Equals(order.UserName, User.Identity?.Name, StringComparison.OrdinalIgnoreCase)) return Forbid();

            if (!order.CheckoutEventId.HasValue)
                return Conflict(new { orderId = id, message = "This legacy order has no basket snapshot and cannot be safely compensated." });

            await _messageBus.SendAsync(new CancelOrderSaga { CheckoutSagaId = id });
            return Accepted(new { orderId = id, status = "Cancellation requested" });
        }
    }
}
