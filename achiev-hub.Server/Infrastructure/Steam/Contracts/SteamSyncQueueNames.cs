namespace achiev_hub.Server.Infrastructure.Steam.Contracts;

/// <summary>RabbitMQ queue names used by Hub when publishing to steam-sync.</summary>
public static class SteamSyncQueueNames
{
    public const string FirstSync = "first_sync";
    public const string FirstSyncDeadLetterExchange = "first_sync_dlx";
    public const string FirstSyncDeadLetterQueue = "first_sync_dlq";
}
