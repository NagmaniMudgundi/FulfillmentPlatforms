using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FulfillmentPlatform.Application.Auth;
using FulfillmentPlatform.Application.Common;
using FulfillmentPlatform.Application.Orders;
using FulfillmentPlatform.Application.Products;
using FulfillmentPlatform.IntegrationTests.Infrastructure;

namespace FulfillmentPlatform.IntegrationTests;

public class OrdersApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string Password = "Passwort1";
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;   // a client WITHOUT a token (= visitor)

    public OrdersApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ───────────────────────── HELPERS ─────────────────────────

    // unique values for every call, so tests never clash
    private static string UniqueEmail(string name) => $"{name}-{Guid.NewGuid():N}@test.de";
    private static string UniqueSlug() => "shop-" + Guid.NewGuid().ToString("N")[..8];

    // like "Authorize" in Swagger
    private HttpClient ClientWithToken(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task<string> LoginAsync(string email)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return login!.Token;
    }

    // a new shop + its logged-in admin
    private async Task<(int TenantId, string Slug, HttpClient Admin)> CreateShopWithAdminAsync()
    {
        var adminEmail = UniqueEmail("admin");
        var slug = UniqueSlug();
        var response = await _client.PostAsJsonAsync("/api/auth/register-company", new
        {
            companyName = "Shop " + slug,
            slug,
            adminEmail,
            adminPassword = Password,
            adminFirstName = "Max",
            adminLastName = "Admin"
        });
        response.EnsureSuccessStatusCode();
        var company = await response.Content.ReadFromJsonAsync<RegisterCompanyResponse>();

        var token = await LoginAsync(adminEmail);
        return (company!.TenantId, slug, ClientWithToken(token));
    }

    // a new logged-in customer (platform account, no shop)
    private async Task<HttpClient> CreateCustomerAsync(string name)
    {
        var email = UniqueEmail(name);
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new { email, password = Password, firstName = name, lastName = "Test" });
        response.EnsureSuccessStatusCode();

        var token = await LoginAsync(email);
        return ClientWithToken(token);
    }

    // POST /api/products as admin → product id
    private static async Task<int> CreateProductAsync(HttpClient admin, bool isActive = true)
    {
        var response = await admin.PostAsJsonAsync("/api/products", new
        {
            name = "TShirt",
            sku = "TS-" + Guid.NewGuid().ToString("N")[..8],
            price = 500,
            stockQuantity = 100,
            categoryId = 1,
            isActive
        });
        response.EnsureSuccessStatusCode();
        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();
        return product!.Id;
    }

    // the checkout body React will send
    private static object CartBody(params (int ProductId, int Quantity)[] items) => new
    {
        items = items.Select(i => new { productId = i.ProductId, quantity = i.Quantity }).ToArray(),
        street = "Lindachstraße 26",
        postalCode = "72764",
        city = "Reutlingen"
    };

    // POST /api/orders/checkout as customer → the receipt
    private static async Task<CheckoutResponse> CheckoutAsync(HttpClient customer, params (int ProductId, int Quantity)[] items)
    {
        var response = await customer.PostAsJsonAsync("/api/orders/checkout", CartBody(items));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CheckoutResponse>())!;
    }

    // ───────────────────────── SECURITY ─────────────────────────

    [Fact]
    public async Task GetOrders_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/orders");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ChangeStatus_AsCustomer_Returns403()
    {
        var (_, _, admin) = await CreateShopWithAdminAsync();
        var productId = await CreateProductAsync(admin);
        var anna = await CreateCustomerAsync("anna");
        var orderId = (await CheckoutAsync(anna, (productId, 1))).Orders.Single().Id;

        var response = await anna.PatchAsJsonAsync($"/api/orders/{orderId}/status", new { newStatus = "Processing" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetOrderById_OtherCustomersOrder_Returns404()
    {
        var (_, _, admin) = await CreateShopWithAdminAsync();
        var productId = await CreateProductAsync(admin);
        var anna = await CreateCustomerAsync("anna");
        var ben = await CreateCustomerAsync("ben");
        var orderId = (await CheckoutAsync(anna, (productId, 1))).Orders.Single().Id;

        var response = await ben.GetAsync($"/api/orders/{orderId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetOrderById_OtherShopsOrder_Returns404()
    {
        // the most important test: shop 2 must never see shop 1's data
        var (_, _, admin1) = await CreateShopWithAdminAsync();
        var productId = await CreateProductAsync(admin1);
        var anna = await CreateCustomerAsync("anna");
        var orderId = (await CheckoutAsync(anna, (productId, 1))).Orders.Single().Id;
        var (_, _, admin2) = await CreateShopWithAdminAsync();

        var response = await admin2.GetAsync($"/api/orders/{orderId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ProductBackOffice_AsCustomer_Returns403()
    {
        var anna = await CreateCustomerAsync("anna");
        var response = await anna.GetAsync("/api/products");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Dashboard_AsCustomer_Returns403()
    {
        var anna = await CreateCustomerAsync("anna");
        var response = await anna.GetAsync("/api/admin/dashboard");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ───────────────────────── CHECKOUT ─────────────────────────

    [Fact]
    public async Task Checkout_AsCustomer_Returns200()
    {
        var (_, _, admin) = await CreateShopWithAdminAsync();
        var productId = await CreateProductAsync(admin);
        var customer = await CreateCustomerAsync("customer");

        var response = await customer.PostAsJsonAsync("/api/orders/checkout", CartBody((productId, 1)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Checkout_CartFromTwoShops_EachShopSeesOnlyItsOwnOrder()
    {
        var (_, _, admin1) = await CreateShopWithAdminAsync();
        var (_, _, admin2) = await CreateShopWithAdminAsync();
        var product1 = await CreateProductAsync(admin1);
        var product2 = await CreateProductAsync(admin2);
        var anna = await CreateCustomerAsync("anna");

        var receipt = await CheckoutAsync(anna, (product1, 1), (product2, 2));

        Assert.Equal(2, receipt.Orders.Count);                         // split per shop

        var annasOrders = await anna.GetFromJsonAsync<PagedResult<OrderResponse>>("/api/orders");
        Assert.Equal(2, annasOrders!.TotalCount);                      // Anna sees both

        var shop1Orders = await admin1.GetFromJsonAsync<PagedResult<OrderResponse>>("/api/orders");
        Assert.Equal(1, shop1Orders!.TotalCount);                      // each shop sees only its own
    }

    [Fact]
    public async Task Checkout_AsAdmin_Returns403()
    {
        var (_, _, admin) = await CreateShopWithAdminAsync();
        var productId = await CreateProductAsync(admin);

        var response = await admin.PostAsJsonAsync("/api/orders/checkout", CartBody((productId, 1)));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Checkout_QuantityZero_Returns400()
    {
        var (_, _, admin) = await CreateShopWithAdminAsync();
        var productId = await CreateProductAsync(admin);
        var anna = await CreateCustomerAsync("anna");

        var response = await anna.PostAsJsonAsync("/api/orders/checkout", CartBody((productId, 0)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ───────────────────────── CATALOG + SHOPS ─────────────────────────

    [Fact]
    public async Task Catalog_AsVisitor_ShowsShopProductsWithoutInternalData()
    {
        var (_, slug, admin) = await CreateShopWithAdminAsync();
        await CreateProductAsync(admin);

        var response = await _client.GetAsync($"/api/catalog/products?shop={slug}");
        var json = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"totalCount\":1", json);
        Assert.DoesNotContain("sku", json, StringComparison.OrdinalIgnoreCase);         // internal data stays internal
        Assert.DoesNotContain("tenantId", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("stockQuantity", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Catalog_InactiveProduct_Returns404()
    {
        var (_, _, admin) = await CreateShopWithAdminAsync();
        var hiddenId = await CreateProductAsync(admin, isActive: false);

        var response = await _client.GetAsync($"/api/catalog/products/{hiddenId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RegisterCompany_DuplicateSlug_Returns409()
    {
        var (_, slug, _) = await CreateShopWithAdminAsync();

        var response = await _client.PostAsJsonAsync("/api/auth/register-company", new
        {
            companyName = "Copy Shop",
            slug,                                                       // already taken
            adminEmail = UniqueEmail("copy"),
            adminPassword = Password,
            adminFirstName = "Copy",
            adminLastName = "Cat"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}