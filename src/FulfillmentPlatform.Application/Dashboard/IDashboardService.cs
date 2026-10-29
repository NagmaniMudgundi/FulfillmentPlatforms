namespace FulfillmentPlatform.Application.Dashboard;

public interface IDashboardService
{
    Task<DashboardResponse> GetAsync();
}