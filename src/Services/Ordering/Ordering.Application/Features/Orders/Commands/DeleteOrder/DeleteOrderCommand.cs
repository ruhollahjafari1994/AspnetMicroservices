using MediatR;

namespace Ordering.Application.Features.Orders.Commands.DeleteOrder
{
    /// <summary>
    /// Defines the DeleteOrderCommand type.
    /// </summary>
    public class DeleteOrderCommand : IRequest
    {
        /// <summary>
        /// Gets or sets the Id value.
        /// </summary>
        public int Id { get; set; }
    }
}
