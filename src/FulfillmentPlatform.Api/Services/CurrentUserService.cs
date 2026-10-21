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

    // the logged-in user of the CURRENT request (or null if there is no request)
    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public string UserId =>
        User?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedException("User is not authenticated.");

    public int TenantId
    {
        get
        {
            var value = User?.FindFirstValue("tenantId");
            if (value is null)
            {
                throw new UnauthorizedException("No tenant found in token.");
            }
            return int.Parse(value);
        }
    }

    
}