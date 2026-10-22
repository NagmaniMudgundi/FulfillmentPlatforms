using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FulfillmentPlatform.Application.Orders
{
    public record CancelOrderRequest
    {
        [MaxLength(500)]
        public string? Reason { get; init; }
    }
}
