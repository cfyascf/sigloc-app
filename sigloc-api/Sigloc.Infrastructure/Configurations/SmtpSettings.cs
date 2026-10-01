namespace Sigloc.Infrastructure.Configurations;

public class SmtpSettings
{
    public const string SectionName = "Smtp";
    public string Host { get; init; } = string.Empty;
    public int Port { get; init; } = 587;
    public bool EnableSsl { get; init; } = true;
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FromAddress { get; init; } = string.Empty;
    public string FromName { get; init; } = "Sigloc";
}
