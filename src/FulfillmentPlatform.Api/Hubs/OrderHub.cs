using Microsoft.Extensions.Logging;
using System.Security.Claims;
using FulfillmentPlatform.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FulfillmentPlatform.Api.Hubs;

[Authorize]                                    // only logged-in users may connect
public class OrderHub : Hub
{
    private readonly ILogger<OrderHub> _logger;
    public OrderHub(ILogger<OrderHub> logger)
    {
        _logger = logger;
    }

    // Runs automatically every time a browser connects
    public override async Task OnConnectedAsync()
    {
        var user = Context.User!;              // the logged-in user (claims from the token)

        // TODO 1: read the user id      → user.FindFirstValue(ClaimTypes.NameIdentifier)
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        
        // TODO 2: read the tenant id    → user.FindFirstValue("tenantId")
        var tenantId = user.FindFirstValue("tenantId");

        // TODO 3: every user joins their PERSONAL group:
        //         await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");


        // TODO 4: if the user is a TenantAdmin, ALSO join the tenant's admin group:
        //         if (user.IsInRole(Roles.TenantAdmin)) → AddToGroupAsync(… , $"tenant-{tenantId}-admins")
        if(user.IsInRole(Roles.TenantAdmin))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant-{tenantId}-admins");
            _logger.LogInformation("Admin {UserId} joined group tenant-{TenantId}-admins", userId, tenantId);
        }

        await base.OnConnectedAsync();
    }
}