using FulfillmentPlatform.Application.Common;
using FulfillmentPlatform.Application.Common.Exceptions;

namespace FulfillmentPlatform.IntegrationTests.Infrastructure;

// only used to create the test database: no request, no user, no shop
public class NoUserCurrentUserService : ICurrentUserService
{
    public string? UserIdOrNull => null;
    public string UserId => string.Empty;
    public int? TenantId => null;                       // was: int TenantId => 0
    public int RequireTenantId() =>
        throw new UnauthorizedException("No shop in test setup.");
    public bool IsInRole(string role) => false;
}