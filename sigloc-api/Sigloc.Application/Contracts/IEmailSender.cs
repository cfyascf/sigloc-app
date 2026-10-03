namespace Sigloc.Application.Contracts;

public interface IEmailSender
{
    Task SendPasswordResetAsync(string recipientEmail, string resetLink, CancellationToken cancellationToken = default);
}
