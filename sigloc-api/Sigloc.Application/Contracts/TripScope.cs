namespace Sigloc.Application.Contracts;

public enum TripParty { Shipper, Carrier }

/// <summary>
/// Tenant boundary for trip monitoring: a shipper sees trips on its own routes, a carrier sees trips
/// it was awarded. A trip outside the scope is reported as not found (never forbidden).
/// </summary>
public readonly record struct TripScope(TripParty Party, Guid CompanyId)
{
    public static TripScope ForShipper(Guid contractorId) => new(TripParty.Shipper, contractorId);
    public static TripScope ForCarrier(Guid carrierId) => new(TripParty.Carrier, carrierId);
}
