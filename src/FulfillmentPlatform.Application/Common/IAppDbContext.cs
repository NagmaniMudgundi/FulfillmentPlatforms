using FulfillmentPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FulfillmentPlatform.Application.Common
{
    public interface IAppDbContext
    {
        DbSet<Tenant> Tenants { get; }
        DbSet<Product> Products { get; }
        DbSet<Order> Orders { get; }
        DbSet<OrderItem> OrderItems { get; }
        DbSet<OrderStatusHistory> OrderStatusHistories { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
