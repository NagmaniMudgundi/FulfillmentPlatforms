using FulfillmentPlatform.Application.Common;
using FulfillmentPlatform.Domain.Entities;
using FulfillmentPlatform.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace FulfillmentPlatform.Infrastructure.Persistence
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>, IAppDbContext
    {
        private readonly ICurrentUserService _currentUser;
        // read fresh for EVERY query, from the current request's token
        public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUserService currentUser) : base(options)
        {
            _currentUser = currentUser;
        }
        private int? CurrentTenantId => _currentUser.TenantId;
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Global query filters: every query is automatically limited to the current tenant
            modelBuilder.Entity<Product>().HasQueryFilter(p => p.TenantId == CurrentTenantId);
            // TODO: the same for Order
            modelBuilder.Entity<Order>().HasQueryFilter(p => p.TenantId == CurrentTenantId);
            // Configure relationships and constraints here if needed
            modelBuilder.Entity<Tenant>().Property(t => t.Name).HasMaxLength(200);

            modelBuilder.Entity<Tenant>().Property(t => t.Slug).IsRequired().HasMaxLength(50);
            modelBuilder.Entity<Tenant>().Property(t => t.Description).HasMaxLength(100);
            modelBuilder.Entity<Tenant>().Property(t => t.City).HasMaxLength(100);
            modelBuilder.Entity<Tenant>().Property(t => t.BrandColor).HasMaxLength(7);
            modelBuilder.Entity<Tenant>().HasIndex(t => t.Slug).IsUnique();

            modelBuilder.Entity<Product>().Property(p => p.Price).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Product>().Property(p=>p.Name).HasMaxLength(200);
            modelBuilder.Entity<Product>().Property(p=>p.Sku).HasMaxLength(100);
            modelBuilder.Entity<Product>().Property(p => p.RowVersion).IsRowVersion();
            modelBuilder.Entity<Order>().Property(o=>o.UserId).HasMaxLength(450);

            modelBuilder.Entity<ApplicationUser>().Property(u => u.FirstName).HasMaxLength(100);
            modelBuilder.Entity<ApplicationUser>().Property(u => u.LastName).HasMaxLength(100);

            modelBuilder.Entity<Order>().Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");

            modelBuilder.Entity<OrderItem>().Property(oi => oi.UnitPrice).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<OrderItem>().HasQueryFilter(i => i.Order.TenantId == CurrentTenantId);
            modelBuilder.Entity<OrderStatusHistory>().HasQueryFilter(h => h.Order.TenantId == CurrentTenantId);
            modelBuilder.Entity<OrderItem>()         // ① "I'm configuring the OrderItem table"
            .HasOne(oi => oi.Product)               // ② "each OrderItem has ONE Product"
            .WithMany(p => p.OrderItems)            // ③ "each Product has MANY OrderItems"
            .HasForeignKey(oi => oi.ProductId)      // ④ "the link column is ProductId"
            .OnDelete(DeleteBehavior.Restrict);     // ⑤ "block deleting a Product that has OrderItems"
             // User belongs to one Tenant. Tenant has no list of users (Domain can't know ApplicationUser)
            modelBuilder.Entity<ApplicationUser>()
                .HasOne(u => u.Tenant)           // the navigation you added in Part A
                .WithMany()                      // empty: no list on the Tenant side
                .HasForeignKey(u => u.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            // Order belongs to one user. Order has no navigation property (Domain can't know ApplicationUser)
            modelBuilder.Entity<Order>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrderStatusHistory>().Property(p=> p.Note).HasMaxLength(500);

            //Indexes for performance optimization and search filtering
            // single column
            modelBuilder.Entity<Order>().HasIndex(o => o.UserId);

            // two columns together (composite), and unique
            modelBuilder.Entity<Product>().HasIndex(p => new { p.TenantId, p.Sku }).IsUnique();

            modelBuilder.Entity<Order>().HasIndex(o => new { o.TenantId, o.Status });

        }
        public DbSet<Tenant> Tenants { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderItem> OrderItems { get; set; } = null!;
        public DbSet<OrderStatusHistory> OrderStatusHistories { get; set; } = null!;
    }

}
