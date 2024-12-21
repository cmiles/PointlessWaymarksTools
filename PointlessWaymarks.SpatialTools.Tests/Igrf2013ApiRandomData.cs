using System.Text.Json.Serialization;
using Flurl.Http;

namespace PointlessWaymarks.SpatialTools.Tests;

public class Igrf2013ApiRandomData
{
    private static readonly Random Random = new();

    public static (double latitude, double longitude, DateOnly date, double elevation) GetRandomLatLongDate()
    {
        var latitude = Math.Round(Random.NextDouble() * 180 - 90, 4); // Latitude between -90 and 90
        var longitude = Math.Round(Random.NextDouble() * 360 - 180, 4); // Longitude between -180 and 180
        var year = Random.Next(1900, DateTime.Now.Year + 1); // Year between 1900 and current year
        var month = Random.Next(1, 13); // Month between 1 and 12
        var day = Random.Next(1,
            DateTime.DaysInMonth(year, month) + 1); // Day between 1 and the number of days in the month
        var elevation = Math.Round(Random.NextDouble() * 10000, 0); // Elevation between 0 and 10,000 meters

        var date = new DateOnly(year, month, day);

        return (latitude, longitude, date, elevation);
    }
}




