using System.ComponentModel;

namespace Sigloc.Domain.Enums;

/// <summary>
/// RNTRC registration category (ANTT Res. 5.982/2022). Sent over the wire as the integer value.
/// </summary>
public enum RntrcCategory
{
    [Description("TAC (Transportador Autônomo)")]
    TAC = 1,

    [Description("ETC (Empresa de Transporte)")]
    ETC = 2,

    [Description("CTC (Cooperativa de Transporte)")]
    CTC = 3
}
