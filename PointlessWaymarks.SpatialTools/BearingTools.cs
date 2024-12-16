namespace PointlessWaymarks.SpatialTools;

public static class BearingTools
{
    public static double ApplyDeclination(double magneticBearing, double declination)
    {
        // Apply the declination to the magnetic bearing
        return NormalizeBearing(NormalizeBearing(magneticBearing) + declination);
    }

    public static double ApplyDeclination(double magneticBearing, IgrfGeomagneticData magneticData)
    {
        // Apply the declination to the magnetic bearing
        return NormalizeBearing(NormalizeBearing(magneticBearing) + magneticData.Declination);
    }

    public static double NormalizeBearing(double bearing)
    {
        // Normalize the bearing to be within the range of 0 to 360 degrees
        bearing = bearing % 360;
        if (bearing < 0) bearing += 360;
        return bearing;
    }
}