using FulfillmentPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FulfillmentPlatform.Infrastructure.Persistence
{
    public class AppDbContext: DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Configure relationships and constraints here if needed
            modelBuilder.Entity<Tenant>().Property(t => t.Name).HasMaxLength(200);

            modelBuilder.Entity<Product>().Property(p => p.Price).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Product>().Property(p=>p.Name).HasMaxLength(200);
            modelBuilder.Entity<Product>().Property(p=>p.Sku).HasMaxLength(100);

            modelBuilder.Entity<Order>().Property(o=>o.UserId).HasMaxLength(450);

            modelBuilder.Entity<Order>().Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");

            modelBuilder.Entity<OrderItem>().Property(oi => oi.UnitPrice).HasColumnType("decimal(18,2)");

            modelBuilder.Entity<OrderItem>()         // ① "I'm configuring the OrderItem table"
            .HasOne(oi => oi.Product)               // ② "each OrderItem has ONE Product"
            .WithMany(p => p.OrderItems)            // ③ "each Product has MANY OrderItems"
            .HasForeignKey(oi => oi.ProductId)      // ④ "the link column is ProductId"
            .OnDelete(DeleteBehavior.Restrict);     // ⑤ "block deleting a Product that has OrderItems"

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
