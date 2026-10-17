using RabbitMQ.Client;
using System.Text;
using System.Threading.Tasks;
using Testcontainers.RabbitMq;
using Xunit;

namespace Ordering.UnitTests;

/// <summary>
/// Verifies RabbitMQ redelivers an unacknowledged durable-queue message after consumer failure.
/// </summary>
public sealed class RabbitMqRedeliveryIntegrationTests : IAsyncLifetime
{
    private readonly RabbitMqContainer _rabbit = new RabbitMqBuilder("rabbitmq:3-management-alpine")
        .WithUsername("integration")
        .WithPassword("integration-only-password")
        .Build();

    /// <inheritdoc />
    public Task InitializeAsync() => _rabbit.StartAsync();

    /// <inheritdoc />
    public Task DisposeAsync() => _rabbit.DisposeAsync().AsTask();

    /// <summary>
    /// Confirms an unacked persistent message becomes available with its redelivered flag set.
    /// </summary>
    [Fact]
    public async Task UnacknowledgedMessage_IsRedeliveredAfterChannelCloses()
    {
        var factory = new ConnectionFactory
        {
            HostName = _rabbit.Hostname,
            Port = _rabbit.GetMappedPublicPort(5672),
            UserName = "integration",
            Password = "integration-only-password"
        };
        await using var connection = await factory.CreateConnectionAsync();
        var firstChannel = await connection.CreateChannelAsync();
        await firstChannel.QueueDeclareAsync("redelivery-test", durable: true, exclusive: false, autoDelete: false);
        await firstChannel.BasicPublishAsync("", "redelivery-test", true, new BasicProperties { Persistent = true }, Encoding.UTF8.GetBytes("test"));
        var firstDelivery = await firstChannel.BasicGetAsync("redelivery-test", autoAck: false);
        Assert.NotNull(firstDelivery);
        await firstChannel.CloseAsync();

        var retryChannel = await connection.CreateChannelAsync();
        var redelivery = await retryChannel.BasicGetAsync("redelivery-test", autoAck: false);
        Assert.NotNull(redelivery);
        Assert.True(redelivery.Redelivered);
        await retryChannel.BasicAckAsync(redelivery.DeliveryTag, multiple: false);
        await retryChannel.CloseAsync();
    }
}
