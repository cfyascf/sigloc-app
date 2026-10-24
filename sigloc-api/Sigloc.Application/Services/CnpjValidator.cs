namespace Sigloc.Application.Services;

/// <summary>
/// Validates a CNPJ (digits only): 14 digits, not all equal, and both check digits correct
/// (Receita Federal modulo-11 rule).
/// </summary>
public static class CnpjValidator
{
    private static readonly int[] FirstWeights = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
    private static readonly int[] SecondWeights = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

    public static bool IsValid(string? cnpj)
    {
        if (cnpj is null || cnpj.Length != 14 || !cnpj.All(char.IsAsciiDigit))
        {
            return false;
        }

        // "11111111111111" and similar pass the arithmetic but are not real registrations.
        if (cnpj.All(c => c == cnpj[0]))
        {
            return false;
        }

        return cnpj[12] - '0' == CheckDigit(cnpj, FirstWeights)
            && cnpj[13] - '0' == CheckDigit(cnpj, SecondWeights);
    }

    private static int CheckDigit(string cnpj, int[] weights)
    {
        var sum = 0;
        for (var i = 0; i < weights.Length; i++)
        {
            sum += (cnpj[i] - '0') * weights[i];
        }

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }
}
