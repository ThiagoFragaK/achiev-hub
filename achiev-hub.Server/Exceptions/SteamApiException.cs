namespace achiev_hub.Server.Exceptions;

public class SteamApiException : Exception
{
    public const string UserMessage = "Steam API is unreachable.";

    public SteamApiException() : base(UserMessage)
    {
    }

    public SteamApiException(string message) : base(message)
    {
    }
}
