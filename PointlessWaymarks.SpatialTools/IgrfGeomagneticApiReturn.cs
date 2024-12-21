using System.Text.Json.Serialization;

namespace PointlessWaymarks.SpatialTools;

public class IgrfGeomagneticApiReturn
{
    [JsonPropertyName("geomagnetic-field-model-result")]
    public GeomagneticFieldResult? GeomagneticFieldModelResult { get; set; }

    public void Validate()
    {
        if (GeomagneticFieldModelResult == null ||
            GeomagneticFieldModelResult.FieldValue == null ||
            GeomagneticFieldModelResult.FieldValue.Declination == null ||
            GeomagneticFieldModelResult.FieldValue.EastIntensity == null ||
            GeomagneticFieldModelResult.FieldValue.HorizontalIntensity == null ||
            GeomagneticFieldModelResult.FieldValue.Inclination == null ||
            GeomagneticFieldModelResult.FieldValue.NorthIntensity == null ||
            GeomagneticFieldModelResult.FieldValue.TotalIntensity == null ||
            GeomagneticFieldModelResult.FieldValue.VerticalIntensity == null)
            throw new InvalidOperationException("Invalid response: One or more required fields are missing.");
    }

    public class Altitude
    {
        [JsonPropertyName("units")] public string Units { get; set; } = string.Empty;

        [JsonPropertyName("value")] public double Value { get; set; }
    }

    public class Coordinates
    {
        [JsonPropertyName("altitude")] public Altitude? Altitude { get; set; }

        [JsonPropertyName("latitude")] public Latitude? Latitude { get; set; }

        [JsonPropertyName("longitude")] public Longitude? Longitude { get; set; }
    }

    public class Date
    {
        [JsonPropertyName("value")] public string Value { get; set; } = string.Empty;
    }

    public class Declination
    {
        [JsonPropertyName("units")] public string Units { get; set; } = string.Empty;

        [JsonPropertyName("value")] public double Value { get; set; }
    }

    public class EastIntensity
    {
        [JsonPropertyName("units")] public string Units { get; set; } = string.Empty;

        [JsonPropertyName("value")] public double Value { get; set; }
    }

    public class FieldValue
    {
        [JsonPropertyName("declination")] public Declination? Declination { get; set; }

        [JsonPropertyName("east-intensity")] public EastIntensity? EastIntensity { get; set; }

        [JsonPropertyName("horizontal-intensity")]
        public HorizontalIntensity? HorizontalIntensity { get; set; }

        [JsonPropertyName("inclination")] public Inclination? Inclination { get; set; }

        [JsonPropertyName("north-intensity")] public NorthIntensity? NorthIntensity { get; set; }

        [JsonPropertyName("total-intensity")] public TotalIntensity? TotalIntensity { get; set; }

        [JsonPropertyName("vertical-intensity")]
        public VerticalIntensity? VerticalIntensity { get; set; }
    }

    public class GeomagneticFieldResult
    {
        [JsonPropertyName("coordinates")] public Coordinates? Coordinates { get; set; }

        [JsonPropertyName("date")] public Date? Date { get; set; }

        [JsonPropertyName("field-value")] public FieldValue? FieldValue { get; set; }

        [JsonPropertyName("model")] public string Model { get; set; } = string.Empty;

        [JsonPropertyName("model_revision")] public string ModelRevision { get; set; } = string.Empty;

        [JsonPropertyName("secular-variation")]
        public SecularVariation? SecularVariation { get; set; }
    }

    public class HorizontalIntensity
    {
        [JsonPropertyName("units")] public string Units { get; set; } = string.Empty;

        [JsonPropertyName("value")] public double Value { get; set; }
    }

    public class Inclination
    {
        [JsonPropertyName("units")] public string Units { get; set; } = string.Empty;

        [JsonPropertyName("value")] public double Value { get; set; }
    }

    public class Latitude
    {
        [JsonPropertyName("units")] public string Units { get; set; } = string.Empty;

        [JsonPropertyName("value")] public double Value { get; set; }
    }

    public class Longitude
    {
        [JsonPropertyName("units")] public string Units { get; set; } = string.Empty;

        [JsonPropertyName("value")] public double Value { get; set; }
    }

    public class NorthIntensity
    {
        [JsonPropertyName("units")] public string Units { get; set; } = string.Empty;

        [JsonPropertyName("value")] public double Value { get; set; }
    }

    public class SecularVariation
    {
        [JsonPropertyName("declination")] public Declination? Declination { get; set; }

        [JsonPropertyName("east-intensity")] public EastIntensity? EastIntensity { get; set; }

        [JsonPropertyName("horizontal-intensity")]
        public HorizontalIntensity? HorizontalIntensity { get; set; }

        [JsonPropertyName("inclination")] public Inclination? Inclination { get; set; }

        [JsonPropertyName("north-intensity")] public NorthIntensity? NorthIntensity { get; set; }

        [JsonPropertyName("total-intensity")] public TotalIntensity? TotalIntensity { get; set; }

        [JsonPropertyName("vertical-intensity")]
        public VerticalIntensity? VerticalIntensity { get; set; }
    }

    public class TotalIntensity
    {
        [JsonPropertyName("units")] public string Units { get; set; } = string.Empty;

        [JsonPropertyName("value")] public double Value { get; set; }
    }

    public class VerticalIntensity
    {
        [JsonPropertyName("units")] public string Units { get; set; } = string.Empty;

        [JsonPropertyName("value")] public double Value { get; set; }
    }
}