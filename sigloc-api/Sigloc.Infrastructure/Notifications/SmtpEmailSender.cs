using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using Sigloc.Application.Contracts;
using Sigloc.Infrastructure.Configurations;

namespace Sigloc.Infrastructure.Notifications;

public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpSettings _settings;

    public SmtpEmailSender(IOptions<SmtpSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task SendPasswordResetAsync(string recipientEmail, string resetLink, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.Host) || string.IsNullOrWhiteSpace(_settings.FromAddress))
        {
            throw new InvalidOperationException("SMTP is not configured.");
        }

        using var client = new SmtpClient(_settings.Host, _settings.Port)
        {
            EnableSsl = _settings.EnableSsl,
            Credentials = string.IsNullOrWhiteSpace(_settings.Username)
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(_settings.Username, _settings.Password)
        };
        using var message = new MailMessage(
            new MailAddress(_settings.FromAddress, _settings.FromName),
            new MailAddress(recipientEmail))
        {
            Subject = "Redefinição de senha do Sigloc",
            Body = $"Use o link a seguir para redefinir sua senha. Ele expira em breve:\n\n{resetLink}",
            IsBodyHtml = false
        };

        await client.SendMailAsync(message, cancellationToken);
    }
}
