using FulfillmentPlatform.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FulfillmentPlatform.Domain.Entities
{
    public class Order
    {
        public int Id { get; set; }
        public int TenantId { get; set; } // Foreign Key to Tenant
        public Tenant Tenant { get; set; } = null!; // Navigation property to Tenant
        public string UserId { get; set; } = string.Empty;
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public decimal TotalAmount { get; set; }
        public List<OrderItem> OrderItems { get; set; } = new();
        public List<OrderStatusHistory> StatusHistory { get; set; } = new();
    }

}
