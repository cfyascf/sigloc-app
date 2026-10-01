namespace Sigloc.Application.Contracts;

public interface IPasswordResetLinkBuilder
{
    int TokenExpiryMinutes { get; }
    string Build(string token);
}
