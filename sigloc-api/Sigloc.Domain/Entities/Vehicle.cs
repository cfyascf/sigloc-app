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
