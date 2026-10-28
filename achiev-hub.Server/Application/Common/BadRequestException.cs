namespace achiev_hub.Server.Application.Common;

public class BadRequestException : Exception
{
    public BadRequestException(string message) : base(message)
    {
    }
}
