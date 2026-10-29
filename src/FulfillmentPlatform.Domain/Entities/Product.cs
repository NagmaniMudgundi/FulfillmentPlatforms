using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FulfillmentPlatform.Domain.Entities
{
    public class Product
    {
        public int Id { get; set; }
        public int TenantId { get; set; }//Foreign Key to Tenant
        public Tenant Tenant { get; set; } = null!;// Navigation property to Tenant

        public string Name { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public List<OrderItem> OrderItems { get; set; } = new();
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; } = true;    // false = hidden from the shop window

    }
}
