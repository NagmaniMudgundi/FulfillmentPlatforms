using FulfillmentPlatform.Application.Common.Messaging;
using FulfillmentPlatform.Infrastructure.Messaging;
using FulfillmentPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FulfillmentPlatform.IntegrationTests.Infrastructure;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // a SEPARATE database only for tests (your dev data stays untouched)
    private const string TestConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=FulfillmentPlatformTestDb;Trusted_Connection=True;TrustServerCertificate=True";

    // ① runs ONCE before the tests of a class: create a fresh, empty test database
    public CustomWebApplicationFactory()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(TestConnectionString)
            .Options;

        using var db = new AppDbContext(options, new NoUserCurrentUserService());
        db.Database.EnsureDeleted();   // delete the old test database (if any)
        db.Database.Migrate();         // create it again with ALL your migrations
    }

    // ② how to start the API for tests
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // use the test database instead of your dev database
        builder.UseSetting("ConnectionStrings:DefaultConnection", TestConnectionString);

        builder.ConfigureTestServices(services =>
        {
            // no RabbitMQ in tests: remove the consumer…
            var consumer = services.Single(d => d.ImplementationType == typeof(OrderStatusChangedConsumer));
            services.Remove(consumer);

            // …and replace the publisher with the fake
            services.RemoveAll<IEventPublisher>();
            services.AddSingleton<IEventPublisher, FakeEventPublisher>();
        });
    }
}