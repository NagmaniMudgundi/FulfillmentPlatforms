using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FulfillmentPlatform.Domain.Entities
{
    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; } // Foreign Key to Order
        public Order Order { get; set; } = null!; // Navigation property to Order
        public int ProductId { get; set; } // Foreign Key to Product
        public Product Product { get; set; } = null!; // Navigation property to Product
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
