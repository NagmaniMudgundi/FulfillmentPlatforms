using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FulfillmentPlatform.Application.Orders
{
    public record CreateOrderRequest
    {
        /// <summary>
        /// Gets or sets the list of order items.
        /// </summary>
        [Required, MinLength(1)]
        public List<CreateOrderItemRequest> Items { get; set; } = new();
    }
}
