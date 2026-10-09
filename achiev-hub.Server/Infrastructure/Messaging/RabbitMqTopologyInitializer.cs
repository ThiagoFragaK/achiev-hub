using achiev_hub.Server.Infrastructure.Messaging.Interfaces;
using achiev_hub.Server.Infrastructure.Startup.Interfaces;
using achiev_hub.Server.Infrastructure.Steam.Contracts;
using RabbitMQ.Client;

namespace achiev_hub.Server.Infrastructure.Messaging;

/// <summary>
/// Declares every queue and dead-letter exchange in <see cref="SteamSyncQueueNames"/> at startup.
/// Arguments must stay identical across deployments: RabbitMQ rejects a redeclare with different
/// arguments (PRECONDITION_FAILED).
/// </summary>
public sealed class RabbitMqTopologyInitializer : IStartupTask
{
    private static readonly (string Queue, string DeadLetterExchange, string DeadLetterQueue)[] QueuePairs =
    [
        (SteamSyncQueueNames.Jobs, SteamSyncQueueNames.DeadLetterExchange, SteamSyncQueueNames.DeadLetterQueue),
        (SteamSyncQueueNames.FirstSync, SteamSyncQueueNames.FirstSyncDeadLetterExchange, SteamSyncQueueNames.FirstSyncDeadLetterQueue),
        (SteamSyncQueueNames.GamesListSync, SteamSyncQueueNames.GamesListSyncDeadLetterExchange, SteamSyncQueueNames.GamesListSyncDeadLetterQueue),
        (SteamSyncQueueNames.UserAchievementsSyncHigh, SteamSyncQueueNames.UserAchievementsSyncHighDeadLetterExchange, SteamSyncQueueNames.UserAchievementsSyncHighDeadLetterQueue),
        (SteamSyncQueueNames.UserAchievementsSyncLow, SteamSyncQueueNames.UserAchievementsSyncLowDeadLetterExchange, SteamSyncQueueNames.UserAchievementsSyncLowDeadLetterQueue),
        (SteamSyncQueueNames.SyncLibrary, SteamSyncQueueNames.SyncLibraryDeadLetterExchange, SteamSyncQueueNames.SyncLibraryDeadLetterQueue)
    ];

    private readonly IRabbitMqConnectionFactory _connectionFactory;
    private readonly ILogger<RabbitMqTopologyInitializer> _logger;

    public RabbitMqTopologyInitializer(
        IRabbitMqConnectionFactory connectionFactory,
        ILogger<RabbitMqTopologyInitializer> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public string Name => "RabbitMQ topology";

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        foreach (var (queue, deadLetterExchange, deadLetterQueue) in QueuePairs)
        {
            await DeclareQueuePairAsync(channel, queue, deadLetterExchange, deadLetterQueue, cancellationToken);
            _logger.LogInformation(
                "Declared queue {Queue} with dead-letter exchange {DeadLetterExchange} -> {DeadLetterQueue}",
                queue,
                deadLetterExchange,
                deadLetterQueue);
        }

        await channel.CloseAsync(cancellationToken);
        await connection.CloseAsync(cancellationToken);
    }

    private static async Task DeclareQueuePairAsync(
        IChannel channel,
        string queue,
        string deadLetterExchange,
        string deadLetterQueue,
        CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(
            exchange: deadLetterExchange,
            type: ExchangeType.Fanout,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: deadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: deadLetterQueue,
            exchange: deadLetterExchange,
            routingKey: string.Empty,
            cancellationToken: cancellationToken);

        var args = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = deadLetterExchange
        };

        await channel.QueueDeclareAsync(
            queue: queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: args,
            cancellationToken: cancellationToken);
    }
}
