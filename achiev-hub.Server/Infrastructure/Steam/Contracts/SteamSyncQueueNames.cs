namespace achiev_hub.Server.Infrastructure.Steam.Contracts;

/// <summary>
/// RabbitMQ queues and dead-letter exchanges this service declares at startup
/// (see <c>RabbitMqTopologyInitializer</c>). The names are the message contract with consumers.
/// </summary>
public static class SteamSyncQueueNames
{
    public const string Jobs = "steam_sync_jobs";
    public const string DeadLetterExchange = "steam_sync_dlx";
    public const string DeadLetterQueue = "steam_sync_jobs_dlq";

    public const string FirstSync = "first_sync";
    public const string FirstSyncDeadLetterExchange = "first_sync_dlx";
    public const string FirstSyncDeadLetterQueue = "first_sync_dlq";

    public const string GamesListSync = "games_list_sync";
    public const string GamesListSyncDeadLetterExchange = "games_list_sync_dlx";
    public const string GamesListSyncDeadLetterQueue = "games_list_sync_dlq";

    public const string UserAchievementsSyncHigh = "user_achievements_sync_high";
    public const string UserAchievementsSyncHighDeadLetterExchange = "user_achievements_sync_high_dlx";
    public const string UserAchievementsSyncHighDeadLetterQueue = "user_achievements_sync_high_dlq";

    public const string UserAchievementsSyncLow = "user_achievements_sync_low";
    public const string UserAchievementsSyncLowDeadLetterExchange = "user_achievements_sync_low_dlx";
    public const string UserAchievementsSyncLowDeadLetterQueue = "user_achievements_sync_low_dlq";

    public const string SyncLibrary = "sync_library";
    public const string SyncLibraryDeadLetterExchange = "sync_library_dlx";
    public const string SyncLibraryDeadLetterQueue = "sync_library_dlq";
}
