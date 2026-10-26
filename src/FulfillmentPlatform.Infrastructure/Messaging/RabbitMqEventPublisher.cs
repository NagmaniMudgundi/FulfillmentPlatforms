using System.Text.Json;
using FulfillmentPlatform.Application.Common.Messaging;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace FulfillmentPlatform.Infrastructure.Messaging;

public class RabbitMqEventPublisher : IEventPublisher
{
    private readonly string _hostName;
    private readonly string _queueName;

    public RabbitMqEventPublisher(IConfiguration config)
    {
        _hostName = config["RabbitMq:HostName"]!;
        _queueName = config["RabbitMq:QueueName"]!;
    }

    public async Task PublishAsync<T>(T message)
    {
        // 1. CONNECT: open the road to RabbitMQ, and a channel to talk on
        var factory = new ConnectionFactory { HostName = _hostName };
        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        // 2. MAKE SURE THE MAILBOX EXISTS (creates it only if missing → idempotent)
        await channel.QueueDeclareAsync(
            queue: _queueName,
            durable: true,       // the queue survives a RabbitMQ restart
            exclusive: false,    // other connections (our consumer!) may use it
            autoDelete: false);  // don't delete it when nobody is connected

        // 3. PACK THE LETTER: C# object → JSON → bytes
        var body = JsonSerializer.SerializeToUtf8Bytes(message);
        var properties = new BasicProperties { Persistent = true };   // message is saved to disk

        // 4. POST IT: default exchange "" + routing key = queue name
        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: _queueName,
            mandatory: false,
            basicProperties: properties,
            body: body);
    }
}