using FulfillmentPlatform.Application.Common;
using FulfillmentPlatform.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FulfillmentPlatform.Application.Dashboard;

public class DashboardService : IDashboardService
{
    private const int Days = 14;
    private const int LowStockLimit = 5;

    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DashboardService(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<DashboardResponse> GetAsync()
    {
        // only shop admins have a dashboard; the query filter limits everything to THEIR shop
        _currentUser.RequireTenantId();

        // ① orders per status (all time), counted in SQL
        var byStatus = await _db.Orders
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        // ② last 14 days: load only the 3 columns we need
        var since = DateTime.UtcNow.Date.AddDays(-(Days - 1));
        var recent = await _db.Orders
            .Where(o => o.CreatedAt >= since)
            .Select(o => new { o.CreatedAt, o.TotalAmount, o.Status })
            .ToListAsync();

        var notCancelled = recent.Where(o => o.Status != OrderStatus.Cancelled).ToList();
        var revenue = notCancelled.Sum(o => o.TotalAmount);

        // ③ one bar per day, including days with 0 € (otherwise the chart has gaps)
        var revenueByDay = Enumerable.Range(0, Days)
            .Select(i => since.AddDays(i))
            .Select(day => new DailyRevenue(
                day,
                notCancelled.Where(o => o.CreatedAt.Date == day).Sum(o => o.TotalAmount)))
            .ToList();

        // ④ products that are almost sold out
        var lowStock = await _db.Products
            .Where(p => p.IsActive && p.StockQuantity <= LowStockLimit)
            .OrderBy(p => p.StockQuantity)
            .Take(5)
            .Select(p => new LowStockProduct(p.Id, p.Name, p.StockQuantity))
            .ToListAsync();

        return new DashboardResponse
        {
            OrdersByStatus = Enum.GetValues<OrderStatus>().ToDictionary(
                s => s.ToString(),
                s => byStatus.FirstOrDefault(b => b.Status == s)?.Count ?? 0),   // statuses with 0 orders too
            Revenue14Days = revenue,
            Orders14Days = recent.Count,
            AverageOrderValue = notCancelled.Count == 0 ? 0 : Math.Round(revenue / notCancelled.Count, 2),
            CancellationRate = recent.Count == 0 ? 0
                : Math.Round((double)recent.Count(o => o.Status == OrderStatus.Cancelled) / recent.Count, 3),
            RevenueByDay = revenueByDay,
            LowStock = lowStock
        };
    }
}