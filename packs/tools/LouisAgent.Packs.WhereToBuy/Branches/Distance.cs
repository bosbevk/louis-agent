namespace LouisAgent.Packs.WhereToBuy.Branches;

/// <summary>How far apart two points on the earth are, in a straight line (the haversine formula).</summary>
public static class Distance
{
    private const double EarthKm = 6371.0;

    /// <summary>The distance in kilometres, to one decimal.</summary>
    public static double Km(double latitude1, double longitude1, double latitude2, double longitude2)
    {
        double dLat = Radians(latitude2 - latitude1);
        double dLon = Radians(longitude2 - longitude1);
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(Radians(latitude1)) * Math.Cos(Radians(latitude2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return Math.Round(EarthKm * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a)), 1);
    }

    /// <summary>Degrees as radians.</summary>
    private static double Radians(double degrees) => degrees * Math.PI / 180;
}
