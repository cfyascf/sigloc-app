using System.ComponentModel;

namespace Sigloc.Domain.Enums;

    public enum OperationalStatus
    {
        LIVRE,

        /// <summary>
        /// No longer set: "in transit" comes from the vehicle's active trip. Kept so the stored
        /// integer values keep their meaning; the API rejects it on update.
        /// </summary>
        EM_TRANSITO,
        MANUTENCAO
    }
