using System.Text;
using System.Text.Json;
using achiev_hub.Server.Application.Steam.Interfaces;
using achiev_hub.Server.Infrastructure.Messaging.Interfaces;
using achiev_hub.Server.Infrastructure.Steam.Contracts;
using RabbitMQ.Client;

namespace achiev_hub.Server.Infrastructure.Steam;

public sealed class SteamSyncClient : ISteamSyncClient, IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IRabbitMqConnectionFactory _connectionFactory;
    private readonly ILogger<SteamSyncClient> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;

    public SteamSyncClient(IRabbitMqConnectionFactory connectionFactory, ILogger<SteamSyncClient> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task PublishFirstSyncAsync(int userId, string steamId, CancellationToken cancellationToken = default)
    {
        var job = new FirstSyncJob
        {
            UserId = userId,
            SteamId = steamId
        };

        var channel = await EnsureChannelAsync(cancellationToken);
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(job, JsonOptions));
        var props = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent
        };

        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: SteamSyncQueueNames.FirstSync,
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Published FirstSync job {JobId} for user {UserId} steam {SteamId}",
            job.JobId,
            userId,
            steamId);
    }

    private async Task<IChannel> EnsureChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true })
        {
            return _channel;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_channel is { IsOpen: true })
            {
                return _channel;
            }

            _connection?.Dispose();
            _channel?.Dispose();

            // Topology is declared once at startup by RabbitMqTopologyInitializer.
            _connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
            return _channel;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _gate.WaitAsync();
        try
        {
            if (_channel is not null)
            {
                await _channel.CloseAsync();
                _channel.Dispose();
                _channel = null;
            }

            if (_connection is not null)
            {
                await _connection.CloseAsync();
                _connection.Dispose();
                _connection = null;
            }
        }
        finally
        {
            _gate.Release();
            _gate.Dispose();
        }
    }
}
