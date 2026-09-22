namespace Sigloc.Application.Services;

/// <summary>
/// O Cnpj é guardado no banco só com dígitos (ex: "12345678000199"), mas a spec do
/// GET /api/parcerias exige o formato mascarado ("12.345.678/0001-90"). Método puro,
/// fácil de testar isoladamente.
/// </summary>
public static class CnpjFormatter
{
    public static string Format(string cnpj)
    {
        var digits = new string(cnpj.Where(char.IsDigit).ToArray());

        if (digits.Length != 14)
        {
            // CNPJ malformado no banco - devolve como veio em vez de quebrar a listagem inteira.
            return cnpj;
        }

        return $"{digits[..2]}.{digits[2..5]}.{digits[5..8]}/{digits[8..12]}-{digits[12..14]}";
    }
}