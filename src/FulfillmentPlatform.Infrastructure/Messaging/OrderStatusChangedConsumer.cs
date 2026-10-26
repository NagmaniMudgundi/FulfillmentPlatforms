using System.Text.Json;
using FulfillmentPlatform.Application.Orders.Events;
using FulfillmentPlatform.Infrastructure.Identity;
using FulfillmentPlatform.Application.Common.Email;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using FulfillmentPlatform.Application.Common.Realtime;

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
    private async Task HandleAsync(OrderStatusChangedEvent orderEvent)
    {
        await _realtimeNotifier.NotifyOrderStatusChangedAsync(orderEvent);
        // find the customer's email (UserManager is Scoped → create a scope)
        using var scope = _scopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByIdAsync(orderEvent.UserId);

        // nobody to write to? then there's nothing to do
        if (user?.Email is null)
        {
            _logger.LogWarning("No email found for user {UserId}, order {OrderId}.", orderEvent.UserId, orderEvent.OrderId);
            return;
        }

        // write the email
        var subject = $"Your order #{orderEvent.OrderId} is now {orderEvent.NewStatus}";

        var body =
            $"Hello,\n\n" +
            $"the status of your order #{orderEvent.OrderId} has changed " +
            $"from {orderEvent.OldStatus} to {orderEvent.NewStatus}.\n";

        if (!string.IsNullOrWhiteSpace(orderEvent.Note))
        {
            body += $"\nNote: {orderEvent.Note}\n";
        }

        body += "\nThank you for your order!";

        // send it (real SMTP → Mailpit)
        await _emailSender.SendAsync(user.Email, subject, body);

        _logger.LogInformation("Email sent to {Email} for order {OrderId}.", user.Email, orderEvent.OrderId);
    }
}