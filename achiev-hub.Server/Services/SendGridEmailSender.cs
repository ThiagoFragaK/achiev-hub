using achiev_hub.Server.Options;
using achiev_hub.Server.Services.Interfaces;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace achiev_hub.Server.Services;

public class SendGridEmailSender : IEmailSender
{
    private readonly SendGridOptions _options;
    private readonly ILogger<SendGridEmailSender> _logger;

    public SendGridEmailSender(IOptions<SendGridOptions> options, ILogger<SendGridEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string subject, string plainTextContent, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogWarning("SendGrid ApiKey is not configured. Skipping email to {Email}.", toEmail);
            throw new InvalidOperationException("Email sending is not configured.");
        }

        var client = new SendGridClient(_options.ApiKey);
        var from = new EmailAddress(_options.FromEmail, _options.FromName);
        var to = new EmailAddress(toEmail);
        var message = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, null);
        var response = await client.SendEmailAsync(message, cancellationToken);

        if ((int)response.StatusCode >= 400)
        {
            var body = await response.Body.ReadAsStringAsync(cancellationToken);
            _logger.LogError("SendGrid failed with {Status}: {Body}", response.StatusCode, body);
            throw new InvalidOperationException("Failed to send verification email.");
        }
    }
}
