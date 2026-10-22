using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FulfillmentPlatform.Application.Orders
{
    public record CreateOrderItemRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "ProductId is required.")]
        public int ProductId { get; set; }
        [Range(1, 1000, ErrorMessage = "Quantity must be between 1 and 1000.")]
        public int Quantity { get; set; }

    }
}
