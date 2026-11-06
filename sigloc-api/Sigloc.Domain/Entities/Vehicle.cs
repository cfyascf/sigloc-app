using Sigloc.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sigloc.Domain.Entities;

    [Table("Vehicle")]
    public class Vehicle : BaseEntity
    {
        public Guid TransportadoraId { get; private set; } // FK
        public string Plate { get; private set; }
        public string Model { get; private set; }
        public decimal CapacityWeight { get; private set; }
        public decimal CapacityVolume { get; private set; }
        public int AxleCount { get; private set; }   // qtd de eixos
        public bool HasCargoSecuring { get; private set; } // possui Fixacao de Carga  
        public VehicleBodyType BodyType { get; private set; }
        public RefrigerationLevel RefrigerationLevel { get; private set; }
        public OperationalStatus Status { get; private set; }
        public bool HasMopp { get; private set; }
        public string Driver { get; private set; }
        public string CurrentLocation { get; private set; }

        /// <summary>Optional positive Traccar device identifier.</summary>
        public long? TraccarDeviceId { get; set; }
        /// <summary>Optional driver contact, never substituted with the carrier phone.</summary>
        public string? DriverPhone { get; set; }

        // Documents. Null on vehicles saved before they were asked for: the fleet shows a warning
        // for each missing one, but only data that is known to be invalid blocks a bid.

        /// <summary>Single truck or combination; combinations need a driver with CNH E.</summary>
        public VehicleComposition? Composition { get; set; }

        /// <summary>Whose RNTRC (ANTT) the vehicle runs under.</summary>
        public RntrcSource? RntrcSource { get; set; }

        /// <summary>Third-party RNTRC (8 digits). Null when the vehicle runs under the carrier's.</summary>
        public string? RntrcNumber { get; set; }

        public RntrcCategory? RntrcCategory { get; set; }

        /// <summary>Holder of the third-party RNTRC: the autonomous driver or the cooperative.</summary>
        public string? RntrcHolderName { get; set; }

        /// <summary>CPF (TAC) or CNPJ (ETC/CTC) of the third-party holder, digits only.</summary>
        public string? RntrcHolderDocument { get; set; }

        /// <summary>Third-party RNTRC revalidation due date (annual, Lei 11.442/2007 as of Lei 15.485/2026).</summary>
        public DateOnly? RntrcValidUntil { get; set; }

        public CnhCategory? DriverCnhCategory { get; set; }

        /// <summary>CNH carries the EAR remark (exerce atividade remunerada), required to drive for pay.</summary>
        public bool? DriverHasEar { get; set; }

        /// <summary>Toxicological exam due date (every 30 months for CNH C, D and E).</summary>
        public DateOnly? DriverToxicologicalExamValidUntil { get; set; }

        /// <summary>MOPP course validity; dangerous cargo needs it in date.</summary>
        public DateOnly? MoppValidUntil { get; set; }

        /// <summary>CIV + CIPP (Inmetro) validity; bulk dangerous cargo needs them in date.</summary>
        public DateOnly? CivCippValidUntil { get; set; }

        // Used by EF Core when loading rows: materialising must not run the creation checks, or a
        // single invalid row already in the database breaks every query that reads it.
        private Vehicle()
        {
            Plate = null!;
            Model = null!;
            Driver = null!;
            CurrentLocation = null!;
        }

        // construtor
        public Vehicle(
            Guid transportadoraId,
            string plate,
            string model,
            int axleCount,
            decimal capacityWeight,
            decimal capacityVolume,
            VehicleBodyType bodyType,
            RefrigerationLevel refrigerationLevel,
            bool hasMopp,
            bool hasCargoSecuring,
            string driver,
            string currentLocation,
            OperationalStatus status = OperationalStatus.LIVRE)
                {
                    // Validações de domínio
                    if (transportadoraId == Guid.Empty) throw new ArgumentException("TransportadoraId é obrigatório");
                    if (string.IsNullOrWhiteSpace(plate)) throw new ArgumentException("Placa é obrigatória");
                    if (axleCount <= 0) throw new ArgumentException("A quantidade de eixos deve ser maior que zero (Cálculo ANTT/Pedágio).");
                    if (capacityWeight <= 0) throw new ArgumentException("A capacidade de peso deve ser maior que zero.");
                    if (capacityVolume <= 0) throw new ArgumentException("A capacidade de volume deve ser maior que zero.");

                    TransportadoraId = transportadoraId;
                    Plate = plate.Replace(" ", "").Replace("-", "").ToUpper();
                    Model = model;
                    AxleCount = axleCount;
                    CapacityWeight = capacityWeight;
                    CapacityVolume = capacityVolume;
                    BodyType = bodyType;
                    RefrigerationLevel = refrigerationLevel;
                    HasMopp = hasMopp;
                    HasCargoSecuring = hasCargoSecuring;
                    Driver = driver;
                    CurrentLocation = currentLocation;
                    Status = status;
                }

        // metodo update
        public void Update(
            string? model, 
            int? axleCount, 
            decimal? capacityWeight, 
            decimal? capacityVolume, 
            VehicleBodyType? bodyType, 
            RefrigerationLevel? refrigerationLevel, 
            bool? hasMopp, 
            bool? hasCargoSecuring, 
            string? driver, 
            string? currentLocation, 
            OperationalStatus? status)
        {
            // Same invariants as the constructor; without them an update could store a row the
            // constructor would never accept.
            if (axleCount is <= 0) throw new ArgumentException("A quantidade de eixos deve ser maior que zero (Cálculo ANTT/Pedágio).");
            if (capacityWeight is <= 0) throw new ArgumentException("A capacidade de peso deve ser maior que zero.");
            if (capacityVolume is <= 0) throw new ArgumentException("A capacidade de volume deve ser maior que zero.");

            if (!string.IsNullOrWhiteSpace(model)) Model = model;
            if (axleCount.HasValue) AxleCount = axleCount.Value;
            if (capacityWeight.HasValue) CapacityWeight = capacityWeight.Value;
            if (capacityVolume.HasValue) CapacityVolume = capacityVolume.Value;
            if (bodyType.HasValue) BodyType = bodyType.Value;
            if (refrigerationLevel.HasValue) RefrigerationLevel = refrigerationLevel.Value;
            if (hasMopp.HasValue) HasMopp = hasMopp.Value;
            if (hasCargoSecuring.HasValue) HasCargoSecuring = hasCargoSecuring.Value;
            if (!string.IsNullOrWhiteSpace(driver)) Driver = driver;
            if (!string.IsNullOrWhiteSpace(currentLocation)) CurrentLocation = currentLocation;
            if (status.HasValue) Status = status.Value;
        }
    }
