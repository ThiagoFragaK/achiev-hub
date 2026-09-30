namespace achiev_hub.Server.Application.Common;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }
}
