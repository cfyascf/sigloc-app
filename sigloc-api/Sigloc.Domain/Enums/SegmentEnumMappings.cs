namespace Sigloc.Domain.Enums;

/// <summary>
/// Maps between the API wire values and the internal <see cref="SegmentStatus"/> enum.
/// The wire values are kept in English to match the JSON contract while the domain
/// model uses the same English identifiers.
/// </summary>
public static class SegmentEnumMappings
{
    private static readonly Dictionary<SegmentStatus, string> StatusToWire = new()
    {
        [SegmentStatus.Available] = "AVAILABLE",
        [SegmentStatus.Routed] = "ROUTED",
        [SegmentStatus.InTransit] = "IN_TRANSIT",
        [SegmentStatus.Completed] = "COMPLETED"
    };

    public static string ToWire(this SegmentStatus value) => StatusToWire[value];

    public static bool TryParseStatus(string? value, out SegmentStatus result)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            foreach (var pair in StatusToWire)
            {
                if (string.Equals(pair.Value, value, StringComparison.OrdinalIgnoreCase))
                {
                    result = pair.Key;
                    return true;
                }
            }
        }

        result = default;
        return false;
    }
}
