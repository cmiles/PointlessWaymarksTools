using NetTopologySuite.Geometries;

namespace PointlessWaymarks.SpatialTools;

public static class PointTools
{
    public static Point ProjectCoordinate(Point startPoint, double bearing, double distance)
    {
        const int radius = 6378001;
        var lat1 = startPoint.Y * (Math.PI / 180);
        var lon1 = startPoint.X * (Math.PI / 180);
        var bearing1 = bearing * (Math.PI / 180);
        var lat2 = Math.Asin(Math.Sin(lat1) * Math.Cos(distance / radius) +
                             Math.Cos(lat1) * Math.Sin(distance / radius) * Math.Cos(bearing1));
        var lon2 = lon1 + Math.Atan2(Math.Sin(bearing1) * Math.Sin(distance / radius) * Math.Cos(lat1),
            Math.Cos(distance / radius) - Math.Sin(lat1) * Math.Sin(lat2));
        return Wgs84Point(lon2 * (180 / Math.PI), lat2 * (180 / Math.PI));
    }

    public static Point Wgs84Point(double x, double y, double z)
    {
        return GeoJsonTools.Wgs84GeometryFactory().CreatePoint(new CoordinateZ(x, y, z));
    }

    public static Point Wgs84Point(double x, double y)
    {
        return GeoJsonTools.Wgs84GeometryFactory().CreatePoint(new Coordinate(x, y));
    }
}