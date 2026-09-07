using System.ComponentModel;

namespace Sigloc.Domain.Enums;

    public enum VehicleBodyType
    {
        [Description("Carga Seca Padrão")]
        Bau = 1,
        
        [Description("Baú Sider (Abertura Lateral)")]
        Sider = 2,
        
        [Description("Baú Frigirífico")]
        GradeBaixa = 3,
        
        [Description("Baú Refrigerado")]
        Frigorifico = 4,
        
        [Description("Carreta Prancha / Aberta")]
        Cacamba = 5
    }
