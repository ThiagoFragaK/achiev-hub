using RabbitMQ.Client;

namespace achiev_hub.Server.Infrastructure.Messaging.Interfaces;

public interface IRabbitMqConnectionFactory
{
    Task<IConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);
}
