namespace achiev_hub.Server.Application.Common;

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }
}
