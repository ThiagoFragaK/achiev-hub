namespace achiev_hub.Server.Application.Common.Interfaces;
public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string plainTextContent, CancellationToken cancellationToken = default);
}
