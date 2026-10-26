using FulfillmentPlatform.Application.Common;

namespace FulfillmentPlatform.IntegrationTests.Infrastructure;

public class NoUserCurrentUserService : ICurrentUserService
{
    public string UserId => string.Empty;
    public int TenantId => 0;
    public bool IsInRole(string role) => false;
}