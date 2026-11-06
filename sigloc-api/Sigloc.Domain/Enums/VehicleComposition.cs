using System.ComponentModel;

namespace Sigloc.Domain.Enums;

/// <summary>
/// How the vehicle is put together. Anything but a single truck is a combination, which needs a
/// driver with CNH E. Sent over the wire as the integer value.
/// </summary>
public enum VehicleComposition
{
    [Description("Caminhão simples")]
    Rigid = 1,

    [Description("Cavalo + carreta")]
    Articulated = 2,

    [Description("Caminhão + reboque")]
    TruckTrailer = 3,

    [Description("Bitrem")]
    Bitrain = 4,

    [Description("Rodotrem")]
    Rodotrain = 5
}
