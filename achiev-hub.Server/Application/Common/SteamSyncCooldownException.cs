namespace achiev_hub.Server.Application.Common;

public class SteamSyncCooldownException : Exception
{
    public int RetryAfterSeconds { get; }

    public SteamSyncCooldownException(int retryAfterSeconds)
        : base(BuildMessage(retryAfterSeconds))
    {
        RetryAfterSeconds = retryAfterSeconds;
    }

    private static string BuildMessage(int retryAfterSeconds)
    {
        var minutes = Math.Max(1, (int)Math.Ceiling(retryAfterSeconds / 60.0));
        return minutes == 1
            ? "Achievements were synced recently. Try again in about 1 minute."
            : $"Achievements were synced recently. Try again in about {minutes} minutes.";
    }
}
