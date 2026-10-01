namespace Sigloc.Infrastructure.Configurations;

public class PasswordResetSettings
{
    public const string SectionName = "PasswordReset";
    public string FrontendBaseUrl { get; init; } = "http://localhost:5173/reset-password";
    public int TokenExpiryMinutes { get; init; } = 30;
}
