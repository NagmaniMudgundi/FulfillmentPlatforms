using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FulfillmentPlatform.Application.Orders
{
    public record OrderResponse
    {
        public int Id { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<OrderItemResponse> Items { get; set; } = new();
        public List<OrderStatusHistoryResponse> StatusHistory { get; set; } = new();
        public Guid CheckoutId { get; init; }
        public string ShopName { get; init; } = string.Empty;
        public string ShopSlug { get; init; } = string.Empty;
    }
}
