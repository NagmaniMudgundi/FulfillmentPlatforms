namespace FulfillmentPlatform.Application.Dashboard;

// everything the admin dashboard shows, in one response
public record DashboardResponse
{
    public Dictionary<string, int> OrdersByStatus { get; init; } = new();   // "Pending": 12, ...
    public decimal Revenue14Days { get; init; }
    public int Orders14Days { get; init; }
    public decimal AverageOrderValue { get; init; }
    public double CancellationRate { get; init; }                           // 0.048 = 4.8 %
    public List<DailyRevenue> RevenueByDay { get; init; } = new();          // the bar chart
    public List<LowStockProduct> LowStock { get; init; } = new();
}

public record DailyRevenue(DateTime Date, decimal Revenue);
public record LowStockProduct(int Id, string Name, int StockQuantity);