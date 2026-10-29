using FulfillmentPlatform.Application.Common;
using FulfillmentPlatform.Domain.Entities;
using FulfillmentPlatform.Infrastructure.Identity;
using FulfillmentPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FulfillmentPlatform.Infrastructure.Seeding;

// Demo data for local development: 3 shops, 5 users, 18 products.
// Idempotent: checks before creating, so it can run on every start.
public static class DevDataSeeder
{
    private const string DemoPassword = "Passwort1";

    // category ids from HasData: 1 Fashion · 2 Shoes · 3 Beauty
    private const int Fashion = 1, Shoes = 2, Beauty = 3;

    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var users = services.GetRequiredService<UserManager<ApplicationUser>>();

        // ① shops
        var modeHaus = await EnsureShopAsync(db, "ModeHaus GmbH", "modehaus", "Fashion", "Reutlingen", "#0F766E");
        var schuhWerk = await EnsureShopAsync(db, "SchuhWerk GmbH", "schuhwerk", "Shoes", "Stuttgart", "#B45309");
        var glowLab = await EnsureShopAsync(db, "GlowLab GmbH", "glowlab", "Natural cosmetics", "Tübingen", "#BE185D");

        // ② users: one admin per shop + two customers
        await EnsureUserAsync(users, "max@modehaus.de", "Max", "Müller", Roles.TenantAdmin, modeHaus.Id);
        await EnsureUserAsync(users, "admin@schuhwerk.de", "Sven", "Schuster", Roles.TenantAdmin, schuhWerk.Id);
        await EnsureUserAsync(users, "admin@glowlab.de", "Greta", "Lang", Roles.TenantAdmin, glowLab.Id);
        await EnsureUserAsync(users, "anna@test.de", "Anna", "Schmidt", Roles.Customer, null);
        await EnsureUserAsync(users, "ben@test.de", "Ben", "Wagner", Roles.Customer, null);

        // ③ products
        await EnsureProductsAsync(db, modeHaus.Id, new[]
        {
            P("Rain Jacket Nordsee", "MH-JKT-RAIN",  89.00m,  3, Fashion, "Light, windproof and waterproof. Taped seams and adjustable hood."),
            P("Wool Coat Alb",       "MH-COAT-WOOL", 149.00m, 5, Fashion, "Warm wool coat for cold Swabian winters."),
            P("Merino Scarf",        "MH-SCARF-MER", 24.50m, 40, Fashion, "Soft merino wool, not itchy."),
            P("Organic T-Shirt",     "MH-TSHIRT-ORG", 19.99m, 80, Fashion, "100 % organic cotton, fair produced."),
            P("Chunky Knit Scarf",   "MH-SCARF-KNIT", 34.00m, 25, Fashion, "Hand-knitted, extra long."),
            P("Long Sleeve Basic",   "MH-LS-BASIC",  29.90m, 60, Fashion, "Everyday long sleeve in five colours."),
        });

        await EnsureProductsAsync(db, schuhWerk.Id, new[]
        {
            P("Leather Chelsea Boot", "SW-BOOT-CHELSEA", 129.00m, 12, Shoes, "Handmade leather boot with elastic side panels."),
            P("Trail Runner",         "SW-TRAIL-RUN",     89.00m,  3, Shoes, "Light running shoe for forest trails."),
            P("Canvas Sneaker",       "SW-SNEAKER-CNV",   49.90m, 40, Shoes, "Classic white canvas sneaker."),
            P("Hiking Boot Alb",      "SW-HIKE-ALB",     139.00m,  8, Shoes, "Waterproof hiking boot with grip sole."),
            P("Wool Slipper",         "SW-SLIPPER-WOOL",  39.00m, 30, Shoes, "Felted wool slippers for home."),
            P("Leather Loafer",       "SW-LOAFER",        99.00m,  2, Shoes, "Elegant loafer for the office."),
        });

        await EnsureProductsAsync(db, glowLab.Id, new[]
        {
            P("Rose Face Serum",       "GL-SERUM-ROSE",  24.90m, 50, Beauty, "Natural serum with rose hip oil."),
            P("Tinted Lip Balm",       "GL-LIP-TINT",     9.90m, 120, Beauty, "Care and a light colour in one."),
            P("Hand Cream Calendula",  "GL-HAND-CAL",    12.50m, 70, Beauty, "Rich cream for dry winter hands."),
            P("Solid Shampoo Bar",     "GL-SHAMPOO-BAR", 11.90m, 4, Beauty, "Plastic-free shampoo, lasts up to 80 washes."),
            P("Aloe Day Cream",        "GL-CREAM-ALOE",  19.90m, 35, Beauty, "Light day cream for every skin type."),
            P("Lavender Body Oil",     "GL-OIL-LAV",     16.90m, 22, Beauty, "Relaxing body oil with organic lavender."),
        });
    }

    // find the shop by slug, or adopt an existing one by name, or create it
    private static async Task<Tenant> EnsureShopAsync(
        AppDbContext db, string name, string slug, string description, string city, string brandColor)
    {
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Slug == slug)
                  ?? await db.Tenants.FirstOrDefaultAsync(t => t.Name == name);

        if (tenant is null)
        {
            tenant = new Tenant { Name = name };
            db.Tenants.Add(tenant);
        }

        tenant.Slug = slug;
        tenant.Description = description;
        tenant.City = city;
        tenant.BrandColor = brandColor;

        await db.SaveChangesAsync();
        return tenant;
    }

    // create the user if missing; fill in empty names; make sure the role is set
    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> users, string email, string firstName, string lastName, string role, int? tenantId)
    {
        var user = await users.FindByEmailAsync(email);

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                TenantId = tenantId
            };

            var result = await users.CreateAsync(user, DemoPassword);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Seeding user {email} failed: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
        else
        {
            // existing user (e.g. from earlier tests): complete the profile, keep the password
            if (string.IsNullOrEmpty(user.FirstName)) user.FirstName = firstName;
            if (string.IsNullOrEmpty(user.LastName)) user.LastName = lastName;
            user.TenantId = tenantId;
            await users.UpdateAsync(user);
        }

        if (!await users.IsInRoleAsync(user, role))
        {
            await users.AddToRoleAsync(user, role);
        }
    }

    // add the products this shop doesn't have yet (matched by SKU)
    private static async Task EnsureProductsAsync(AppDbContext db, int tenantId, Product[] products)
    {
        var existingSkus = await db.Products
            .IgnoreQueryFilters()                         // startup has no user → look at the raw table
            .Where(p => p.TenantId == tenantId)
            .Select(p => p.Sku)
            .ToListAsync();

        foreach (var product in products.Where(p => !existingSkus.Contains(p.Sku)))
        {
            product.TenantId = tenantId;
            db.Products.Add(product);
        }

        await db.SaveChangesAsync();
    }

    private static Product P(string name, string sku, decimal price, int stock, int categoryId, string description) => new()
    {
        Name = name,
        Sku = sku,
        Price = price,
        StockQuantity = stock,
        CategoryId = categoryId,
        Description = description,
        IsActive = true
    };
}