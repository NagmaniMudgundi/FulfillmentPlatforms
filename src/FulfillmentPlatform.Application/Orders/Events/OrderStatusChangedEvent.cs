using FulfillmentPlatform.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FulfillmentPlatform.Application.Orders.Events
{
    public record OrderStatusChangedEvent
    {
        public int OrderId { get; init; }
        public int TenantId { get; init; }
        public string UserId { get; init; } = string.Empty;
        public string OldStatus { get; init; } = string.Empty;
        public string NewStatus { get; init; } = string.Empty;
        public string? Note { get; init; }
        public DateTime ChangedAt { get; init; } = DateTime.UtcNow;
    }
}
