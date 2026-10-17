using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AspnetRunBasics.Models;
using AspnetRunBasics.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AspnetRunBasics
{
    /// <summary>
    /// Defines the OrderModel type.
    /// </summary>
    public class OrderModel : PageModel
    {
        private readonly IOrderService _orderService;

        /// <summary>
        /// Describes the OrderModel operation.
        /// </summary>
        public OrderModel(IOrderService orderService)
        {
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
        }

        /// <summary>
        /// Gets or sets the Orders value.
        /// </summary>
        public IEnumerable<OrderResponseModel> Orders { get; set; } = new List<OrderResponseModel>();

        /// <summary>
        /// Describes the OnGetAsync operation.
        /// </summary>
        public async Task<IActionResult> OnGetAsync()
        {
            Orders = await _orderService.GetOrdersByUserName(User.Identity!.Name!);

            return Page();
        }

        /// <summary>
        /// Describes the OnPostCancelAsync operation.
        /// </summary>
        public async Task<IActionResult> OnPostCancelAsync(int orderId)
        {
            await _orderService.CancelOrder(orderId);
            TempData["OrderCancellationRequested"] = true;
            return RedirectToPage();
        }
    }
}
