
namespace FulfillmentPlatform.Application.Common;

public interface ICurrentUserService
{
    string UserId { get; }
    int TenantId { get; }
}