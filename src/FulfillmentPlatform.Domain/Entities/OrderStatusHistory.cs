using FulfillmentPlatform.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FulfillmentPlatform.Domain.Entities
{
    public class OrderStatusHistory
    {
        public int Id { get; set; }

        public int OrderId { get; set; } // Foreign Key to Order
        public Order Order { get; set; } = null!; // Navigation property to Order
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
        public string? Note { get; set; } 

    }
}
