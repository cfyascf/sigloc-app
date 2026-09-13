using Sigloc.Domain.Constants;

namespace Sigloc.Domain.Enums;

/// <summary>
/// Maps between the <see cref="ProfileType"/> enum, the API wire values defined by
/// the specification (uppercase Portuguese) and the RBAC role names used by the
/// authorization layer.
/// </summary>
public static class ProfileTypeMappings
{
    private static readonly Dictionary<ProfileType, string> ProfileToWire = new()
    {
        [ProfileType.Contratante] = "CONTRATANTE",
        [ProfileType.Transportador] = "TRANSPORTADOR",
        [ProfileType.Admin] = "ADMIN"
    };

    private static readonly Dictionary<ProfileType, string> ProfileToRole = new()
    {
        [ProfileType.Contratante] = Roles.Shipper,
        [ProfileType.Transportador] = Roles.Carrier,
        [ProfileType.Admin] = Roles.Admin
    };

    /// <summary>Wire value carried by the API contract (e.g. "CONTRATANTE").</summary>
    public static string ToWire(this ProfileType value) => ProfileToWire[value];

    /// <summary>RBAC role name placed in the JWT role claim (e.g. "Shipper").</summary>
    public static string ToRole(this ProfileType value) => ProfileToRole[value];
}
