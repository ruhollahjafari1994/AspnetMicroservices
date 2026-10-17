using System.Collections.Generic;

namespace Basket.API.Entities
{
    /// <summary>
    /// Defines the ShoppingCart type.
    /// </summary>
    public class ShoppingCart
    {
        /// <summary>
        /// Gets or sets the UserName value.
        /// </summary>
        public string UserName { get; set; }
        /// <summary>
        /// Gets or sets the Items value.
        /// </summary>
        public List<ShoppingCartItem> Items { get; set; } = new List<ShoppingCartItem>();

        /// <summary>
        /// Describes the ShoppingCart operation.
        /// </summary>
        public ShoppingCart()
        {
        }

        /// <summary>
        /// Describes the ShoppingCart operation.
        /// </summary>
        public ShoppingCart(string userName)
        {
            UserName = userName;
        }
        
        /// <summary>
        /// Documents this public member.
        /// </summary>
        public decimal TotalPrice 
        { 
            get
            {
                decimal totalprice = 0;
                foreach (var item in Items)
                {
                    totalprice += item.Price * item.Quantity;
                }
                return totalprice;
            }
        }
    }
}
