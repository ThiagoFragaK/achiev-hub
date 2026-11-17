namespace achiev_hub.Server.Application.Common;

public class SteamSyncCooldownException : Exception
{
    public int RetryAfterSeconds { get; }

    public SteamSyncCooldownException(int retryAfterSeconds, string? message = null)
        : base(message ?? BuildSpacingMessage(retryAfterSeconds))
    {
        RetryAfterSeconds = retryAfterSeconds;
    }

    public static SteamSyncCooldownException DailyLimitReached(int retryAfterSeconds)
    {
        return new SteamSyncCooldownException(
            retryAfterSeconds,
            "Daily manual sync limit reached (10 per day). Try again tomorrow.");
    }

    public static SteamSyncCooldownException SpacingNotElapsed(int retryAfterSeconds)
    {
        return new SteamSyncCooldownException(retryAfterSeconds, BuildSpacingMessage(retryAfterSeconds));
    }

    private static string BuildSpacingMessage(int retryAfterSeconds)
    {
        var minutes = Math.Max(1, (int)Math.Ceiling(retryAfterSeconds / 60.0));
        return minutes == 1
            ? "Game stats were synced recently. Try again in about 1 minute."
            : $"Game stats were synced recently. Try again in about {minutes} minutes.";
    }
}
