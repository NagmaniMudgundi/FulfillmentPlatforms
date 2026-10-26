using FulfillmentPlatform.Application.Common.Realtime;
using FulfillmentPlatform.Application.Orders.Events;
using Microsoft.AspNetCore.SignalR;

namespace FulfillmentPlatform.Api.Hubs;

public class SignalRRealtimeNotifier : IRealtimeNotifier
{
    private readonly IHubContext<OrderHub> _hubContext;

    public SignalRRealtimeNotifier(IHubContext<OrderHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyOrderStatusChangedAsync(OrderStatusChangedEvent orderEvent)
    {
        // TODO: send to BOTH groups at once:

        //   await _hubContext.Client
        //       .Groups($"user-{orderEvent.UserId}", $"tenant-{orderEvent.TenantId}-admins")
        //       .SendAsync("OrderStatusChanged", orderEvent);
        await _hubContext.Clients
          .Groups($"user-{orderEvent.UserId}", $"tenant-{orderEvent.TenantId}-admins")
          .SendAsync("OrderStatusChanged", orderEvent);
    }
}