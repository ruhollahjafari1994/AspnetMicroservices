using System.Collections.Generic;


namespace Basket.API.Entities
{
    public class ShoppingCard
    {

        public string  UserName { get; set; }

        public List<ShoppingCardItem> Items { get; set; } = new List<ShoppingCardItem>();

        public ShoppingCard()
        {

        }
        public ShoppingCard(string userName)
        {
            UserName = userName;
        }
        public decimal TotalPrice
        {
            get
            {
                decimal totalprice = 0;
                foreach (var item in Items)
                {
                    totalprice += item.Price + item.Quality;
                }
                return totalprice;
            }
        }
    }
}
