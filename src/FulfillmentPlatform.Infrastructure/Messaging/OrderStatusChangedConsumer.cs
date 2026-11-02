using FulfillmentPlatform.Application.Common.Email;
using FulfillmentPlatform.Application.Common.Realtime;
using FulfillmentPlatform.Application.Orders.Events;
using FulfillmentPlatform.Infrastructure.Identity;
using FulfillmentPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text.Json;

namespace FulfillmentPlatform.Infrastructure.Messaging;

public class OrderStatusChangedConsumer : BackgroundService
{
    private readonly IConfiguration _config;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrderStatusChangedConsumer> _logger;
    private readonly IEmailSender _emailSender;
    private readonly IRealtimeNotifier _realtimeNotifier;

    public OrderStatusChangedConsumer(
    IConfiguration config,
    IServiceScopeFactory scopeFactory,
    ILogger<OrderStatusChangedConsumer> logger,
    IEmailSender emailSender,
    IRealtimeNotifier realtimeNotifier)
    {
        _config = config;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _emailSender = emailSender;
        _realtimeNotifier = realtimeNotifier;
    }

    // Runs ONCE when the API starts, and keeps listening until the API stops
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var queueName = _config["RabbitMq:QueueName"]!;

        // 1. CONNECT (same as the publisher)
        var factory = new ConnectionFactory { HostName = _config["RabbitMq:HostName"]! };
        await using var connection = await factory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        // 2. MAKE SURE THE MAILBOX EXISTS (same settings as the publisher!)
        await channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false,
            cancellationToken: stoppingToken);

        // 3. WHAT TO DO WHEN A MESSAGE ARRIVES
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (sender, delivery) =>
        {
            try
            {
                // bytes → JSON → C# object
                var orderEvent = JsonSerializer.Deserialize<OrderStatusChangedEvent>(delivery.Body.Span)!;

                await HandleAsync(orderEvent);                                   // ← YOUR code

                await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false);   // ✅ "done, delete it"
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle message. It will be delivered again.");
                await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: true);  // ❌ "put it back"
            }
        };

        // 4. START LISTENING (autoAck: false → WE decide when a message is done)
        await channel.BasicConsumeAsync(queueName, autoAck: false, consumer: consumer,
            cancellationToken: stoppingToken);

        // 5. KEEP RUNNING until the API shuts down
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    // Handles ONE message: "send" the notification
    // Handles ONE message: send the notification email
    // Handles ONE message: live update + email to the customer
    private async Task HandleAsync(OrderStatusChangedEvent orderEvent)
    {
        // ① live update in the browser (customer + shop admins)
        await _realtimeNotifier.NotifyOrderStatusChangedAsync(orderEvent);

        // UserManager and AppDbContext are Scoped → create a scope
        using var scope = _scopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // ② who gets the email?
        var user = await userManager.FindByIdAsync(orderEvent.UserId);
        if (user?.Email is null)
        {
            _logger.LogWarning("No email found for user {UserId}, order {OrderId}.", orderEvent.UserId, orderEvent.OrderId);
            return;
        }

        // ③ which shop? (the event only carries the TenantId)
        var shopName = await db.Tenants
            .Where(t => t.Id == orderEvent.TenantId)
            .Select(t => t.Name)
            .FirstOrDefaultAsync() ?? "our shop";

        var greeting = string.IsNullOrWhiteSpace(user.FirstName) ? "Hello," : $"Hello {user.FirstName},";

        // ④ new order or status update?
        var isNewOrder = orderEvent.OldStatus == "None";

        string subject;
        string body;

        if (isNewOrder)
        {
            subject = $"Order confirmation #{orderEvent.OrderId} from {shopName}";
            body =
                $"{greeting}\n\n" +
                $"thank you for your order #{orderEvent.OrderId} at {shopName}!\n" +
                $"We have received it and will start processing it soon.\n\n" +
                $"You can follow every step live under \"My orders\".\n";
        }
        else
        {
            subject = $"Your order #{orderEvent.OrderId} from {shopName} is now {orderEvent.NewStatus}";
            body =
                $"{greeting}\n\n" +
                $"the status of your order #{orderEvent.OrderId} at {shopName} " +
                $"has changed from {orderEvent.OldStatus} to {orderEvent.NewStatus}.\n";

            if (!string.IsNullOrWhiteSpace(orderEvent.Note))
            {
                body += $"\nNote: {orderEvent.Note}\n";
            }
        }

        body += $"\nBest regards,\nYour {shopName} team";

        // ⑤ send it (SMTP → Mailpit)
        await _emailSender.SendAsync(user.Email, subject, body);

        _logger.LogInformation("Email '{Subject}' sent to {Email}.", subject, user.Email);
    }
}