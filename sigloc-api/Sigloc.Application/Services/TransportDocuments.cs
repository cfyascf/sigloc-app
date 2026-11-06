namespace Sigloc.Application.Services;

/// <summary>Format checks for the documents of road freight: RNTRC and CPF.</summary>
public static class TransportDocuments
{
    /// <summary>
    /// The 8-digit RNTRC, or null when the text isn't one. ANTT's public lookup sometimes shows it
    /// with 9 digits and a leading zero, which CT-e and MDF-e drop.
    /// </summary>
    public static string? NormalizeRntrc(string? text)
    {
        var digits = new string((text ?? "").Where(char.IsAsciiDigit).ToArray());
        if (digits.Length == 9 && digits[0] == '0')
        {
            digits = digits[1..];
        }

        return digits.Length == 8 && digits != "00000000" && text!.All(c => char.IsAsciiDigit(c) || c is ' ' or '.' or '-')
            ? digits
            : null;
    }

    /// <summary>CPF (digits only): 11 digits, not all equal, both check digits correct.</summary>
    public static bool IsValidCpf(string? cpf)
    {
        if (cpf is null || cpf.Length != 11 || !cpf.All(char.IsAsciiDigit) || cpf.All(c => c == cpf[0]))
        {
            return false;
        }

        return cpf[9] - '0' == CpfCheckDigit(cpf, 9) && cpf[10] - '0' == CpfCheckDigit(cpf, 10);
    }

    private static int CpfCheckDigit(string cpf, int length)
    {
        var sum = 0;
        for (var i = 0; i < length; i++)
        {
            sum += (cpf[i] - '0') * (length + 1 - i);
        }

        var remainder = sum * 10 % 11;
        return remainder == 10 ? 0 : remainder;
    }
}
