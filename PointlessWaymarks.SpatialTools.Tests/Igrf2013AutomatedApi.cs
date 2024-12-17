using System.Text.Json.Serialization;
using Flurl.Http;

namespace PointlessWaymarks.SpatialTools.Tests;

public class Igrf2013AutomatedApi
{
    private static readonly Random Random = new();

    public static async Task<(string url, IgrfGeomagneticData result)> GetIgrfMagneticDataAsync(IgrfGeomagneticData reference)
    {
        return await GetIgrfMagneticDataAsync(reference.Latitude, reference.Longitude, reference.AltitudeInMeters,
            reference.Date);
    }

    public static async Task<(string url, IgrfGeomagneticData result)> GetIgrfMagneticDataAsync(double latitude,
        double longitude, double altitude, DateOnly date)
    {
        var dateString = date.ToString("yyyy-MM-dd");
        var url =
            $"http://geomag.bgs.ac.uk/web_service/GMModels/igrf/13/?latitude={latitude}&longitude={longitude}&altitude={altitude/1000}&date={dateString}&format=json";

        TestContext.WriteLine(url);

        var response = await url.GetJsonAsync<IgrfMagneticDataObject>();

        return new ValueTuple<string, IgrfGeomagneticData>(url, new IgrfGeomagneticData
        {
            AltitudeInMeters = altitude, // Convert km to meters
            Date = date,
            Latitude = latitude,
            Longitude = longitude,
            Declination = response.GeomagneticFieldModelResult.FieldValue.Declination.Value,
            EastComponent = response.GeomagneticFieldModelResult.FieldValue.EastIntensity.Value,
            HorizontalIntensity = response.GeomagneticFieldModelResult.FieldValue.HorizontalIntensity.Value,
            Inclination = response.GeomagneticFieldModelResult.FieldValue.Inclination.Value,
            NorthComponent = response.GeomagneticFieldModelResult.FieldValue.NorthIntensity.Value,
            SecularVariationDeclination = response.GeomagneticFieldModelResult.SecularVariation.Declination.Value,
            SecularVariationEast = response.GeomagneticFieldModelResult.SecularVariation.EastIntensity.Value,
            SecularVariationHorizontalIntensity =
                response.GeomagneticFieldModelResult.SecularVariation.HorizontalIntensity.Value,
            SecularVariationInclination = response.GeomagneticFieldModelResult.SecularVariation.Inclination.Value,
            SecularVariationNorth = response.GeomagneticFieldModelResult.SecularVariation.NorthIntensity.Value,
            SecularVariationTotalIntensity = response.GeomagneticFieldModelResult.SecularVariation.TotalIntensity.Value,
            SecularVariationVertical = response.GeomagneticFieldModelResult.SecularVariation.VerticalIntensity.Value,
            TotalIntensity = response.GeomagneticFieldModelResult.FieldValue.TotalIntensity.Value,
            VerticalComponent = response.GeomagneticFieldModelResult.FieldValue.VerticalIntensity.Value
        });
    }

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

public class IgrfMagneticDataObject
{
    [JsonPropertyName("geomagnetic-field-model-result")]
    public GeomagneticFieldResult GeomagneticFieldModelResult { get; set; }

    public class Altitude
    {
        [JsonPropertyName("units")] public string Units { get; set; }

        [JsonPropertyName("value")] public double Value { get; set; }
    }

    public class Coordinates
    {
        [JsonPropertyName("altitude")] public Altitude Altitude { get; set; }

        [JsonPropertyName("latitude")] public Latitude Latitude { get; set; }

        [JsonPropertyName("longitude")] public Longitude Longitude { get; set; }
    }

    public class Date
    {
        [JsonPropertyName("value")] public string Value { get; set; }
    }

    public class Declination
    {
        [JsonPropertyName("units")] public string Units { get; set; }

        [JsonPropertyName("value")] public double Value { get; set; }
    }

    public class EastIntensity
    {
        [JsonPropertyName("units")] public string Units { get; set; }

        [JsonPropertyName("value")] public double Value { get; set; }
    }

    public class FieldValue
    {
        [JsonPropertyName("declination")] public Declination Declination { get; set; }

        [JsonPropertyName("east-intensity")] public EastIntensity EastIntensity { get; set; }

        [JsonPropertyName("horizontal-intensity")]
        public HorizontalIntensity HorizontalIntensity { get; set; }

        [JsonPropertyName("inclination")] public Inclination Inclination { get; set; }

        [JsonPropertyName("north-intensity")] public NorthIntensity NorthIntensity { get; set; }

        [JsonPropertyName("total-intensity")] public TotalIntensity TotalIntensity { get; set; }

        [JsonPropertyName("vertical-intensity")]
        public VerticalIntensity VerticalIntensity { get; set; }
    }

    public class GeomagneticFieldResult
    {
        [JsonPropertyName("coordinates")] public Coordinates Coordinates { get; set; }

        [JsonPropertyName("date")] public Date Date { get; set; }

        [JsonPropertyName("field-value")] public FieldValue FieldValue { get; set; }

        [JsonPropertyName("model")] public string Model { get; set; }

        [JsonPropertyName("model_revision")] public string ModelRevision { get; set; }

        [JsonPropertyName("secular-variation")]
        public SecularVariation SecularVariation { get; set; }
    }

    public class HorizontalIntensity
    {
        [JsonPropertyName("units")] public string Units { get; set; }

        [JsonPropertyName("value")] public double Value { get; set; }
    }

    public class Inclination
    {
        [JsonPropertyName("units")] public string Units { get; set; }

        [JsonPropertyName("value")] public double Value { get; set; }
    }

    public class Latitude
    {
        [JsonPropertyName("units")] public string Units { get; set; }

        [JsonPropertyName("value")] public double Value { get; set; }
    }

    public class Longitude
    {
        [JsonPropertyName("units")] public string Units { get; set; }

        [JsonPropertyName("value")] public double Value { get; set; }
    }

    public class NorthIntensity
    {
        [JsonPropertyName("units")] public string Units { get; set; }

        [JsonPropertyName("value")] public double Value { get; set; }
    }

    public class SecularVariation
    {
        [JsonPropertyName("declination")] public Declination Declination { get; set; }

        [JsonPropertyName("east-intensity")] public EastIntensity EastIntensity { get; set; }

        [JsonPropertyName("horizontal-intensity")]
        public HorizontalIntensity HorizontalIntensity { get; set; }

        [JsonPropertyName("inclination")] public Inclination Inclination { get; set; }

        [JsonPropertyName("north-intensity")] public NorthIntensity NorthIntensity { get; set; }

        [JsonPropertyName("total-intensity")] public TotalIntensity TotalIntensity { get; set; }

        [JsonPropertyName("vertical-intensity")]
        public VerticalIntensity VerticalIntensity { get; set; }
    }

    public class TotalIntensity
    {
        [JsonPropertyName("units")] public string Units { get; set; }

        [JsonPropertyName("value")] public double Value { get; set; }
    }

    public class VerticalIntensity
    {
        [JsonPropertyName("units")] public string Units { get; set; }

        [JsonPropertyName("value")] public double Value { get; set; }
    }
}