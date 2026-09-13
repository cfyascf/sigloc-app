namespace Sigloc.Application.Exceptions;

/// <summary>
/// Raised during smart onboarding (Cenário B) when the carrier CNPJ already
/// exists. The registration is aborted and the front-end must redirect the
/// carrier to the login screen to authenticate and only then accept the pending
/// partnership. Produces a 409 response.
/// </summary>
public sealed class CarrierAlreadyRegisteredException : Exception
{
    public CarrierAlreadyRegisteredException(string cnpj)
        : base($"Já existe uma transportadora cadastrada com o CNPJ '{cnpj}'. Faça login para confirmar a parceria.")
    {
    }
}
