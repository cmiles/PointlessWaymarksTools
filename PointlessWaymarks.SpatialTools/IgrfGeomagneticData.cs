using System.Text;

namespace PointlessWaymarks.SpatialTools;

public class IgrfGeomagneticData
{
    public double AltitudeInMeters { get; set; }
    public DateOnly Date { get; set; }
    public double Declination { get; set; }
    public double EastComponent { get; set; }
    public double HorizontalIntensity { get; set; }
    public double Inclination { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double NorthComponent { get; set; }
    public double SecularVariationDeclination { get; set; }
    public double SecularVariationEast { get; set; }
    public double SecularVariationHorizontalIntensity { get; set; }
    public double SecularVariationInclination { get; set; }
    public double SecularVariationNorth { get; set; }
    public double SecularVariationTotalIntensity { get; set; }
    public double SecularVariationVertical { get; set; }
    public double TotalIntensity { get; set; }
    public double VerticalComponent { get; set; }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Date: {Date}");
        sb.AppendLine($"Latitude: {Latitude}°");
        sb.AppendLine($"Longitude: {Longitude}°");
        sb.AppendLine($"AltitudeInMeters: {AltitudeInMeters}m");
        sb.AppendLine($"Declination: {Declination}°");
        sb.AppendLine($"Inclination: {Inclination}°");
        sb.AppendLine($"Horizontal Intensity (H): {HorizontalIntensity} nT");
        sb.AppendLine($"Total Intensity (F): {TotalIntensity} nT");
        sb.AppendLine($"North Component (X): {NorthComponent} nT");
        sb.AppendLine($"East Component (Y): {EastComponent} nT");
        sb.AppendLine($"Vertical Component (Z): {VerticalComponent} nT");
        sb.AppendLine($"Secular Variation Declination (D): {SecularVariationDeclination} arcmin/yr");
        sb.AppendLine($"Secular Variation Inclination (I): {SecularVariationInclination} arcmin/yr");
        sb.AppendLine($"Secular Variation Horizontal Intensity (H): {SecularVariationHorizontalIntensity} nT/yr");
        sb.AppendLine($"Secular Variation Total Intensity (F): {SecularVariationTotalIntensity}  nT/yr");
        sb.AppendLine($"Secular Variation North (X): {SecularVariationNorth}  nT/yr");
        sb.AppendLine($"Secular Variation East (Y): {SecularVariationEast} nT/yr");
        sb.AppendLine($"Secular Variation Vertical (Z): {SecularVariationVertical} nT/yr");
        return sb.ToString();
    }
}