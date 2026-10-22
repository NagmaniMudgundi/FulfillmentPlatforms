using FulfillmentPlatform.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FulfillmentPlatform.Domain.Rules
{
    public static class OrderStatusTransitions
    {
        // The table: for each status, which statuses may come next
        private static readonly Dictionary<OrderStatus, OrderStatus[]> Allowed = new()
        {
            [OrderStatus.Pending] = new[] { OrderStatus.Processing, OrderStatus.Cancelled },
            // TODO 1: Processing → Shipped, Cancelled
            [OrderStatus.Processing] = new[] { OrderStatus.Shipped, OrderStatus.Cancelled },
            // TODO 2: Shipped    → Delivered
            [OrderStatus.Shipped] = new[] { OrderStatus.Delivered },
            // TODO 3: Delivered  → nothing
            [OrderStatus.Delivered] = new OrderStatus[0],
            // TODO 4: Cancelled  → nothing
            [OrderStatus.Cancelled] = new OrderStatus[0]
        };

        // The question: "may an order go from 'from' to 'to'?"
        public static bool CanTransition(OrderStatus from, OrderStatus to)
        {
            // TODO 5: return Allowed[from].Contains(to);
            return Allowed.TryGetValue(from, out var allowedStatuses) && allowedStatuses.Contains(to);
        }
    }
}
