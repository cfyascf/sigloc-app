using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Sigloc.Application.Contracts;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;

namespace Sigloc.Infrastructure.Services;

public sealed class AnttFreightFloorService(HttpClient httpClient, ILogger<AnttFreightFloorService> logger) : IAnttFreightFloorService
{
    private static readonly CultureInfo BrazilianCulture = CultureInfo.GetCultureInfo("pt-BR");
    private static readonly Regex TokenPattern = new("name=\"__RequestVerificationToken\" type=\"hidden\"\\s+value=\"(?<token>[^\"]+)\"", RegexOptions.Compiled);
    private static readonly Regex MinimumFreightPattern = new("Valor de ida =.*?<span[^>]*>\\s*(?<value>[\\d.,]+)", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex DisplacementPattern = new("\\(CCD\\).*?<span[^>]*>\\s*(?<value>[\\d.,]+)", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex LoadingPattern = new("\\(CC\\).*?<span[^>]*>\\s*(?<value>[\\d.,]+)", RegexOptions.Compiled | RegexOptions.Singleline);

    public async Task<AnttFreightFloorResult?> CalculateAsync(double distanceKm, IReadOnlyCollection<Product> products, int axleCount, CancellationToken cancellationToken = default)
    {
        if (distanceKm <= 0 || axleCount <= 0) return null;

        try
        {
            var page = await httpClient.GetStringAsync("/", cancellationToken);
            var token = TokenPattern.Match(page).Groups["token"].Value;
            if (string.IsNullOrWhiteSpace(token))
            {
                logger.LogWarning("ANTT calculator page did not provide an antiforgery token.");
                return null;
            }

            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = WebUtility.HtmlDecode(token),
                ["Filtro.IdTipoCarga"] = ResolveCargoType(products).ToString(CultureInfo.InvariantCulture),
                ["Filtro.NumeroEixos"] = NormalizeAxles(axleCount).ToString(CultureInfo.InvariantCulture),
                ["Filtro.Distancia"] = Math.Ceiling(distanceKm).ToString(CultureInfo.InvariantCulture),
                ["Filtro.CargaLotacao"] = "true",
                ["Filtro.AltoDesempenho"] = "false",
                ["Filtro.RetornoVazio"] = "false"
            });

            using var response = await httpClient.PostAsync("/?Length=4", content, cancellationToken);
            response.EnsureSuccessStatusCode();
            var calculation = await response.Content.ReadAsStringAsync(cancellationToken);

            return TryParse(calculation, MinimumFreightPattern, out var minimumFreight)
                && TryParse(calculation, DisplacementPattern, out var displacement)
                && TryParse(calculation, LoadingPattern, out var loading)
                ? new AnttFreightFloorResult(minimumFreight, displacement, loading, DateTimeOffset.UtcNow)
                : null;
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Unable to retrieve the official ANTT freight floor.");
            return null;
        }
    }

    private static bool TryParse(string html, Regex pattern, out decimal value) =>
        decimal.TryParse(WebUtility.HtmlDecode(pattern.Match(html).Groups["value"].Value).Trim(), NumberStyles.Number, BrazilianCulture, out value);

    private static int NormalizeAxles(int axleCount) => axleCount switch { <= 2 => 2, 3 => 3, 4 => 4, 5 => 5, 6 => 6, 7 => 7, _ => 9 };

    private static int ResolveCargoType(IReadOnlyCollection<Product> products)
    {
        if (products.Any(product => product.Dangerous)) return 11;
        if (products.Any(product => product.TransportEnvironment != TransportEnvironment.Dry)) return 3;
        if (products.Any(product => product.Category == ProductCategory.LiquidBulk)) return 2;
        if (products.Any(product => product.Category == ProductCategory.SolidBulk)) return 1;
        return 5;
    }
}
