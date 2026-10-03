using System.Text;
using System.Text.Json;
using achiev_hub.Server.Application.Steam.Interfaces;
using achiev_hub.Server.Infrastructure.Options;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using SteamSync.Shared.Messages;

namespace achiev_hub.Server.Infrastructure.Steam;

public sealed class SteamSyncClient : ISteamSyncClient, IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RabbitMqOptions _options;
    private readonly ILogger<SteamSyncClient> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;

    public SteamSyncClient(IOptions<RabbitMqOptions> options, ILogger<SteamSyncClient> logger)
    {
        _options = options.Value;
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
            routingKey: SyncQueueNames.FirstSync,
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

            var factory = CreateFactory();
            _connection = await factory.CreateConnectionAsync(cancellationToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
            await DeclareFirstSyncTopologyAsync(_channel, cancellationToken);
            return _channel;
        }
        finally
        {
            _gate.Release();
        }
    }

    private ConnectionFactory CreateFactory()
    {
        if (!string.IsNullOrWhiteSpace(_options.Url))
        {
            return new ConnectionFactory { Uri = new Uri(_options.Url) };
        }

        return new ConnectionFactory
        {
            HostName = _options.Host,
            Port = _options.Port,
            UserName = _options.Username,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost
        };
    }

    private static async Task DeclareFirstSyncTopologyAsync(IChannel channel, CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(
            exchange: SyncQueueNames.FirstSyncDeadLetterExchange,
            type: ExchangeType.Fanout,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: SyncQueueNames.FirstSyncDeadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: SyncQueueNames.FirstSyncDeadLetterQueue,
            exchange: SyncQueueNames.FirstSyncDeadLetterExchange,
            routingKey: string.Empty,
            cancellationToken: cancellationToken);

        var args = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = SyncQueueNames.FirstSyncDeadLetterExchange
        };

        await channel.QueueDeclareAsync(
            queue: SyncQueueNames.FirstSync,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: args,
            cancellationToken: cancellationToken);
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
