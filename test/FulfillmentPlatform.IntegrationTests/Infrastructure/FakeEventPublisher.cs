using FulfillmentPlatform.Application.Common.Messaging;

namespace FulfillmentPlatform.IntegrationTests.Infrastructure;

// Pretends to publish, does nothing (no RabbitMQ needed in tests)
public class FakeEventPublisher : IEventPublisher
{
    public Task PublishAsync<T>(T message) => Task.CompletedTask;
}