namespace achiev_hub.Server.Services.Interfaces;

public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string plainTextContent, CancellationToken cancellationToken = default);
}
