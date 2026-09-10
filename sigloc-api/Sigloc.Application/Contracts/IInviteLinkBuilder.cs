namespace Sigloc.Application.Contracts;

/// <summary>
/// Builds the shareable B2B link a contractor sends to a carrier from an invite token.
/// The concrete implementation knows the frontend base URL.
/// </summary>
public interface IInviteLinkBuilder
{
    string Build(string token);
}
