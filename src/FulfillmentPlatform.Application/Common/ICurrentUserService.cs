
namespace FulfillmentPlatform.Application.Common;

public interface ICurrentUserService
{
    string UserId { get; }
    int? TenantId { get; }          // null for customers and visitors
    int RequireTenantId();          // for admin-only code: throws if there is no shop
    bool IsInRole(string role);
}