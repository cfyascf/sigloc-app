namespace Sigloc.Application.Exceptions;

public sealed class InvalidPasswordResetTokenException : Exception
{
    public InvalidPasswordResetTokenException()
        : base("O link de redefinição de senha é inválido ou expirou.")
    {
    }
}
