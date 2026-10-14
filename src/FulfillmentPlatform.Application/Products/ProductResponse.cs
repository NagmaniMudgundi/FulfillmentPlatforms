using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FulfillmentPlatform.Application.Products
{
    public record ProductResponse
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public int TenantId { get; init; }
        public string Sku { get; init; } = string.Empty;
        public decimal Price { get; init; }
        public int StockQuantity { get; init; }
    }
}
