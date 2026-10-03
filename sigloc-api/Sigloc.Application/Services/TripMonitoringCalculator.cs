using Sigloc.Application.Contracts;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Enums;
namespace Sigloc.Application.Services;

public static class TripMonitoringCalculator
{
    public static bool IsFresh(TripMonitoring? snapshot, DateTimeOffset now) =>
        snapshot?.LastSuccessfulCalculationAt is { } time && time <= now && now - time < TimeSpan.FromMinutes(15);
    public static bool IsTerminal(Trip trip) => trip.Status is TripStatus.Delivered or TripStatus.Cancelled;
    public static string Status(Trip trip, TripMonitoring? snapshot) => trip.Status switch
    {
        TripStatus.Delivered => "ENTREGUE", TripStatus.Cancelled => "CANCELADA",
        _ when snapshot?.Risk == "CRITIC" => "ATRASADO",
        TripStatus.InTransit => "EM_TRANSITO", _ => "AGUARDANDO_COLETA"
    };
    public static (double Traveled, double Progress) Progress(double total, double remaining, bool finished)
    {
        if (!double.IsFinite(total) || total <= 0) return (0, finished ? 100 : 0);
        var traveled = finished ? total : Math.Clamp(total - remaining, 0, total);
        return (traveled, Math.Clamp(traveled / total * 100, 0, 100));
    }
    public static double? Utilization(double volume, decimal capacity)
    {
        if (capacity <= 0 || !double.IsFinite(volume) || volume < 0) return null;
        var percentage = volume / (double)capacity * 100;
        return double.IsFinite(percentage) ? percentage : null;
    }
    public static double DistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        static double Rad(double x) => x * Math.PI / 180;
        var a = Math.Pow(Math.Sin(Rad(lat2 - lat1) / 2), 2)
            + Math.Cos(Rad(lat1)) * Math.Cos(Rad(lat2)) * Math.Pow(Math.Sin(Rad(lon2 - lon1) / 2), 2);
        return 6371000 * 2 * Math.Asin(Math.Sqrt(Math.Clamp(a, 0, 1)));
    }
}
