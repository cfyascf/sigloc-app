using System.ComponentModel;

namespace Sigloc.Domain.Enums;

    /// <summary>
    /// Vehicle body (carroceria). Persisted and sent over the wire as the integer value, so existing
    /// values never change and new ones are appended. What each body carries lives in
    /// <see cref="Entities.CargoCompatibility"/>.
    /// </summary>
    public enum VehicleBodyType
    {
        [Description("Baú")]
        Bau = 1,

        [Description("Sider")]
        Sider = 2,

        [Description("Grade Baixa")]
        GradeBaixa = 3,

        [Description("Baú Refrigerado")]
        Frigorifico = 4,

        [Description("Caçamba")]
        Cacamba = 5,

        [Description("Graneleiro")]
        Graneleiro = 6,

        [Description("Prancha")]
        Prancha = 7,

        [Description("Tanque")]
        Tanque = 8,

        [Description("Cegonheiro")]
        Cegonheiro = 9,

        [Description("Porta-Contêiner")]
        PortaConteiner = 10,

        [Description("Silo")]
        Silo = 11,

        [Description("Gaiola / Boiadeiro")]
        Gaiola = 12
    }
