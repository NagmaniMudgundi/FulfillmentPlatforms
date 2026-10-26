using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FulfillmentPlatform.Application.Auth;
using FulfillmentPlatform.Application.Orders;
using FulfillmentPlatform.Application.Products;
using FulfillmentPlatform.IntegrationTests.Infrastructure;

namespace FulfillmentPlatform.IntegrationTests;

public class OrdersApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string Password = "Passwort1";
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;   // a client WITHOUT a token

    public OrdersApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ───────────────────────── HELPERS ─────────────────────────

    // a unique email for every call, so tests never clash
    private static string UniqueEmail(string name) => $"{name}-{Guid.NewGuid():N}@test.de";

    // like "Authorize" in Swagger: a client that sends this token with every request
    private HttpClient ClientWithToken(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    // POST /api/auth/login → token
    private async Task<string> LoginAsync(string email)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        response.EnsureSuccessStatusCode();                               // fail the test if not 2xx
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return login!.Token;
    }

    // POST /api/auth/register-company → (tenantId, logged-in admin client)
    private async Task<(int TenantId, HttpClient Admin)> CreateShopWithAdminAsync()
    {
        var adminEmail = UniqueEmail("admin");
        var response = await _client.PostAsJsonAsync("/api/auth/register-company",
            new { companyName = "Shop " + Guid.NewGuid().ToString("N")[..6], adminEmail, adminPassword = Password });
        response.EnsureSuccessStatusCode();
        var company = await response.Content.ReadFromJsonAsync<RegisterCompanyResponse>();

        var token = await LoginAsync(adminEmail);
        return (company!.TenantId, ClientWithToken(token));
    }

    // POST /api/auth/register + login → a logged-in customer client
    private async Task<HttpClient> CreateCustomerAsync(int tenantId, string name)
    {
        var email = UniqueEmail(name);
        var response = await _client.PostAsJsonAsync("/api/auth/register", new { email, password = Password, tenantId });
        response.EnsureSuccessStatusCode();

        var token = await LoginAsync(email);
        return ClientWithToken(token);
    }

    // POST /api/products (as admin) → product id
    private static async Task<int> CreateProductAsync(HttpClient admin)
    {
        var response = await admin.PostAsJsonAsync("/api/products",
            new { name = "TShirt", sku = "TS-" + Guid.NewGuid().ToString("N")[..8], price = 500, stockQuantity = 100 });
        response.EnsureSuccessStatusCode();
        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();
        return product!.Id;
    }

    // POST /api/orders (as customer) → order id
    private static async Task<int> PlaceOrderAsync(HttpClient customer, int productId)
    {
        var response = await customer.PostAsJsonAsync("/api/orders",
            new { items = new[] { new { productId, quantity = 1 } } });
        response.EnsureSuccessStatusCode();
        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        return order!.Id;
    }

    // ───────────────────────── TESTS ─────────────────────────

    [Fact]
    public async Task GetOrders_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/orders");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Test 1 (complete example) ──
    [Fact]
    public async Task ChangeStatus_AsCustomer_Returns403()
    {
        // Arrange: a shop, a product, Anna, and Anna's order
        var (tenantId, admin) = await CreateShopWithAdminAsync();
        var productId = await CreateProductAsync(admin);
        var anna = await CreateCustomerAsync(tenantId, "anna");
        var orderId = await PlaceOrderAsync(anna, productId);

        // Act: Anna tries to change the status herself
        var response = await anna.PatchAsJsonAsync($"/api/orders/{orderId}/status", new { newStatus = "Processing" });

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);   // 403
    }

    // TODO Test 2: GetOrderById_OtherCustomersOrder_Returns404
    //   Arrange: one shop + product, TWO customers (anna, ben), Anna places an order
    //   Act:     Ben calls GET /api/orders/{Anna's orderId}
    //   Assert:  HttpStatusCode.NotFound
    [Fact]
    public async Task GetOrderById_OtherCustomersOrder_Returns404()
    {
        // Arrange: one shop + product, TWO customers (anna, ben), Anna places an order
        var (tenantId, admin) = await CreateShopWithAdminAsync();
        var productId = await CreateProductAsync(admin);
        var anna = await CreateCustomerAsync(tenantId, "anna");
        var ben = await CreateCustomerAsync(tenantId, "ben");
        var orderId = await PlaceOrderAsync(anna, productId);
        // Act: Ben calls GET /api/orders/{Anna's orderId}
        var response = await ben.GetAsync($"/api/orders/{orderId}");
        // Assert: HttpStatusCode.NotFound
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // TODO Test 3: GetOrderById_OtherTenantsOrder_Returns404
    //   Arrange: TWO shops (each CreateShopWithAdminAsync), Anna in shop 1 places an order
    //   Act:     the ADMIN of shop 2 calls GET /api/orders/{Anna's orderId}
    //   Assert:  NotFound
    [Fact]
    public async Task GetOrderById_OtherTenantsOrder_Returns404()
    {
        // Arrange: TWO shops (each CreateShopWithAdminAsync), Anna in shop 1 places an order
        var (tenantId1, admin1) = await CreateShopWithAdminAsync();
        var productId1 = await CreateProductAsync(admin1);
        var anna = await CreateCustomerAsync(tenantId1, "anna");
        var orderId = await PlaceOrderAsync(anna, productId1);
        var (tenantId2, admin2) = await CreateShopWithAdminAsync();
        // Act: the ADMIN of shop 2 calls GET /api/orders/{Anna's orderId}
        var response = await admin2.GetAsync($"/api/orders/{orderId}");
        // Assert: NotFound
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // TODO Test 4: PlaceOrder_AsCustomer_Returns201
    //   Arrange: shop + product + customer
    //   Act:     customer POST /api/orders (same body as in PlaceOrderAsync)
    //   Assert:  HttpStatusCode.Created
    [Fact]
    public async Task PlaceOrder_AsCustomer_Returns201()
    {
        // Arrange: shop + product + customer
        var (tenantId, admin) = await CreateShopWithAdminAsync();
        var productId = await CreateProductAsync(admin);
        var customer = await CreateCustomerAsync(tenantId, "customer");
        // Act: customer POST /api/orders (same body as in PlaceOrderAsync)
        var response = await customer.PostAsJsonAsync("/api/orders",
            new { items = new[] { new { productId, quantity = 1 } } });
        // Assert: HttpStatusCode.Created
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}