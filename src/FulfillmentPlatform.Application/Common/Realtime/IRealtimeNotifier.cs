using FulfillmentPlatform.Application.Orders.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FulfillmentPlatform.Application.Common.Realtime
{
    public interface IRealtimeNotifier
    {
        Task NotifyOrderStatusChangedAsync(OrderStatusChangedEvent orderEvent);
    }
}
