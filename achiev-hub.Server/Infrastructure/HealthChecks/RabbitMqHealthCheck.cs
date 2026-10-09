using achiev_hub.Server.Infrastructure.HealthChecks.Interfaces;
using achiev_hub.Server.Infrastructure.Messaging.Interfaces;
using RabbitMQ.Client;

namespace achiev_hub.Server.Infrastructure.HealthChecks;

public sealed class RabbitMqHealthCheck : IStartupHealthCheck
{
    private readonly IRabbitMqConnectionFactory _connectionFactory;

    public RabbitMqHealthCheck(IRabbitMqConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public string Name => "RabbitMQ";

    public async Task<StartupHealthCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
            await channel.CloseAsync(cancellationToken);
            await connection.CloseAsync(cancellationToken);
            return StartupHealthCheckResult.Healthy();
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            var detail = ex.InnerException is null ? ex.Message : $"{ex.Message} ({ex.InnerException.Message})";
            return StartupHealthCheckResult.Unhealthy(
                $"{detail} Check RabbitMQ__Host, Port, Username, Password, and VirtualHost (or RabbitMQ__Url).",
                ex);
        }
    }
}
