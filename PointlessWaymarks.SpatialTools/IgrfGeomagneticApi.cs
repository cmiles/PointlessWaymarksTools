using Flurl.Http;

namespace PointlessWaymarks.SpatialTools;

public class IgrfGeomagneticApi
{
    private static readonly Random Random = new();

    public static async Task<(string url, IgrfGeomagneticData result)> GetIgrfMagneticDataAsync(
        IgrfGeomagneticData reference)
    {
        return await GetIgrfMagneticDataAsync(reference.Latitude, reference.Longitude, reference.AltitudeInMeters,
            reference.Date);
    }

    public static async Task<(string url, IgrfGeomagneticData result)> GetIgrfMagneticDataAsync(double latitude,
        double longitude, double altitude, DateOnly date)
    {
        var dateString = date.ToString("yyyy-MM-dd");
        var url =
            $"http://geomag.bgs.ac.uk/web_service/GMModels/igrf/13/?latitude={latitude:F5}&longitude={longitude:F5}&altitude={(altitude / 1000):F1}&date={dateString}&format=json";

        var response = await url.GetJsonAsync<IgrfGeomagneticApiReturn>();
        response.Validate();

        return new ValueTuple<string, IgrfGeomagneticData>(url, new IgrfGeomagneticData
        {
            AltitudeInMeters = altitude,
            Date = date,
            Latitude = latitude,
            Longitude = longitude,
            Declination = response.GeomagneticFieldModelResult!.FieldValue!.Declination!.Value,
            EastComponent = response.GeomagneticFieldModelResult.FieldValue.EastIntensity!.Value,
            HorizontalIntensity = response.GeomagneticFieldModelResult.FieldValue.HorizontalIntensity!.Value,
            Inclination = response.GeomagneticFieldModelResult.FieldValue.Inclination!.Value,
            NorthComponent = response.GeomagneticFieldModelResult.FieldValue.NorthIntensity!.Value,
            SecularVariationDeclination =
                response.GeomagneticFieldModelResult.SecularVariation?.Declination?.Value ?? 0,
            SecularVariationEast = response.GeomagneticFieldModelResult.SecularVariation?.EastIntensity?.Value ?? 0,
            SecularVariationHorizontalIntensity =
                response.GeomagneticFieldModelResult.SecularVariation?.HorizontalIntensity?.Value ?? 0,
            SecularVariationInclination =
                response.GeomagneticFieldModelResult.SecularVariation?.Inclination?.Value ?? 0,
            SecularVariationNorth = response.GeomagneticFieldModelResult.SecularVariation?.NorthIntensity?.Value ?? 0,
            SecularVariationTotalIntensity =
                response.GeomagneticFieldModelResult.SecularVariation?.TotalIntensity?.Value ?? 0,
            SecularVariationVertical =
                response.GeomagneticFieldModelResult.SecularVariation?.VerticalIntensity?.Value ?? 0,
            TotalIntensity = response.GeomagneticFieldModelResult.FieldValue.TotalIntensity!.Value,
            VerticalComponent = response.GeomagneticFieldModelResult.FieldValue.VerticalIntensity!.Value
        });
    }
}