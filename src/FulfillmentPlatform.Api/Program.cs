using FulfillmentPlatform.Api.ExceptionHandling;
using FulfillmentPlatform.Api.Hubs;
using FulfillmentPlatform.Api.Services;
using FulfillmentPlatform.Application.Auth;
using FulfillmentPlatform.Application.Catalog;
using FulfillmentPlatform.Application.Common;
using FulfillmentPlatform.Application.Common.Email;
using FulfillmentPlatform.Application.Common.Messaging;
using FulfillmentPlatform.Application.Common.Realtime;
using FulfillmentPlatform.Application.Orders;
using FulfillmentPlatform.Application.Products;
using FulfillmentPlatform.Application.Tenants;
using FulfillmentPlatform.Infrastructure.Email;
using FulfillmentPlatform.Infrastructure.Identity;
using FulfillmentPlatform.Infrastructure.Messaging;
using FulfillmentPlatform.Infrastructure.Persistence;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)     // levels come from appsettings.json
    .Enrich.FromLogContext()                           // adds extra context (e.g. request id) to each log
    .WriteTo.Console()                                 // sink 1: the console window
    .WriteTo.File("logs/log-.txt",                     // sink 2: a file
        rollingInterval: RollingInterval.Day));        //   → a NEW file every day: log-20261026.txt
// Add services to the container.
//What it does: the JSON converter now translates "Shipped" ⇄ OrderStatus.Shipped, in both directions.
builder.Services.AddControllers()
.AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // 1. Tell Swagger: "this API uses Bearer tokens"
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste only the token, without 'Bearer '"
    });

    // 2. Tell Swagger: "send that token with every request"
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddDbContext<AppDbContext>(options=>options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        // Password rules
        options.Password.RequiredLength = 8;
        // TODO: require at least one digit
        options.Password.RequireDigit = true;
        // TODO: require at least one uppercase letter
        options.Password.RequireUppercase = true;
        // TODO: do NOT require a special character (!, ?, #…)
        options.Password.RequireNonAlphanumeric = false;

        // User rules
        // TODO: every email must be unique
        options.User.RequireUniqueEmail = true;

        
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<JwtTokenGenerator>();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],

            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
        // SignalR: WebSockets can't send headers, so the token comes in the URL (?access_token=...)
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                // only for the hub, never for normal API endpoints
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IOrderService, OrderService>();
//use Hangfire, and store jobs in this database” (the same connection string as EF Core)
builder.Services.AddHangfire(config =>
    config.UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));
//starts the worker inside your API that picks up and runs jobs
builder.Services.AddHangfireServer();
builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
builder.Services.AddHostedService<OrderStatusChangedConsumer>();
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
builder.Services.AddSingleton<IRealtimeNotifier, SignalRRealtimeNotifier>();
builder.Services.AddSignalR();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICatalogService, CatalogService>();
var app = builder.Build();

app.UseHangfireDashboard("/hangfire");
var autoCancelDays = builder.Configuration.GetValue<int>("Orders:AutoCancelAfterDays");

var recurringJobs = app.Services.GetRequiredService<IRecurringJobManager>();
recurringJobs.AddOrUpdate<IOrderService>(
    "cancel-stale-orders",                                   // unique name of the job
    service => service.CancelStaleOrdersAsync(autoCancelDays),
    Cron.Hourly);                                            // every hour
using (var scope = app.Services.CreateScope())
{
    await RoleSeeder.SeedAsync(scope.ServiceProvider);
}
app.UseExceptionHandler();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<OrderHub>("/hubs/orders");

app.Run();
public partial class Program { }