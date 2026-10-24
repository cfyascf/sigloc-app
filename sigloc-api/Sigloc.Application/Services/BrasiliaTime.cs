using System.Globalization;

namespace Sigloc.Application.Services;

/// <summary>
/// Deadlines are stored in UTC; labels the API formats for users are shown in Brasília time
/// (UTC−3; Brazil has had no daylight saving time since 2019).
/// </summary>
internal static class BrasiliaTime
{
    private static readonly TimeSpan Offset = TimeSpan.FromHours(-3);
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public static string Format(DateTimeOffset value)
        => value.ToOffset(Offset).ToString("dd/MM/yyyy HH:mm", PtBr);
}
