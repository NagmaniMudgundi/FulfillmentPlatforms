using System.Security.Claims;
using FulfillmentPlatform.Application.Common;
using FulfillmentPlatform.Application.Common.Exceptions;


namespace FulfillmentPlatform.Api.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    public string? UserIdOrNull => User?.FindFirstValue(ClaimTypes.NameIdentifier);
    // the logged-in user of the CURRENT request (or null if there is no request)
    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public string UserId =>
        User?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedException("User is not authenticated.");

    public int? TenantId
    {
        get
        {
            var value = User?.FindFirstValue("tenantId");
            return int.TryParse(value, out var id) ? id : null;   // no badge → null, no crash
        }
    }

    public int RequireTenantId() =>
        TenantId ?? throw new UnauthorizedException("This action requires a shop account.");
    public bool IsInRole(string role) => User?.IsInRole(role) ?? false;

}