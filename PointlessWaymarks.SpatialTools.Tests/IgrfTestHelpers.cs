namespace PointlessWaymarks.SpatialTools.Tests;

public static class IgrfTestHelpers
{
    private static readonly Random Random = new();

    public static void AssertGeomagneticDataEqual(IgrfGeomagneticData expected, IgrfGeomagneticData actual,
        string sourceNote = "None")
    {
        var toleranceDeclination = 0.01;
        var toleranceInclination = 0.01;
        var toleranceHorizontalIntensity = 1.0;
        var toleranceTotalIntensity = 1.0;
        var toleranceNorthComponent = 1.0;
        var toleranceEastComponent = 1.0;
        var toleranceVerticalComponent = 1.0;
        var toleranceSecularVariation = 0.1;

        Assert.Multiple(() =>
        {
            Assert.That(actual.Date, Is.EqualTo(expected.Date), $"Date mismatch. Note:  {sourceNote}");
            Assert.That(actual.Latitude, Is.EqualTo(expected.Latitude).Within(0.000001),
                $"Latitude mismatch. Note:  {sourceNote}");
            Assert.That(actual.Longitude, Is.EqualTo(expected.Longitude).Within(0.000001),
                $"Longitude mismatch. Note:  {sourceNote}");
            Assert.That(actual.AltitudeInMeters, Is.EqualTo(expected.AltitudeInMeters).Within(0.01),
                $"Altitude mismatch. Note:  {sourceNote}");
            Assert.That(actual.Declination, Is.EqualTo(expected.Declination).Within(toleranceDeclination),
                $"Declination mismatch. Note:  {sourceNote}");
            Assert.That(actual.Inclination, Is.EqualTo(expected.Inclination).Within(toleranceInclination),
                $"Inclination mismatch. Note:  {sourceNote}");
            Assert.That(actual.HorizontalIntensity,
                Is.EqualTo(expected.HorizontalIntensity).Within(toleranceHorizontalIntensity),
                $"Horizontal Intensity mismatch. Note:  {sourceNote}");
            Assert.That(actual.TotalIntensity, Is.EqualTo(expected.TotalIntensity).Within(toleranceTotalIntensity),
                $"Total Intensity mismatch. Note:  {sourceNote}");
            Assert.That(actual.NorthComponent, Is.EqualTo(expected.NorthComponent).Within(toleranceNorthComponent),
                $"North Component mismatch. Note:  {sourceNote}");
            Assert.That(actual.EastComponent, Is.EqualTo(expected.EastComponent).Within(toleranceEastComponent),
                $"East Component mismatch. Note:  {sourceNote}");
            Assert.That(actual.VerticalComponent,
                Is.EqualTo(expected.VerticalComponent).Within(toleranceVerticalComponent),
                $"Vertical Component mismatch. Note:  {sourceNote}");
            Assert.That(actual.SecularVariationDeclination,
                Is.EqualTo(expected.SecularVariationDeclination).Within(toleranceSecularVariation),
                $"Secular Variation Declination mismatch. Note:  {sourceNote}");
            Assert.That(actual.SecularVariationInclination,
                Is.EqualTo(expected.SecularVariationInclination).Within(toleranceSecularVariation),
                $"Secular Variation Inclination mismatch. Note:  {sourceNote}");
            Assert.That(actual.SecularVariationHorizontalIntensity,
                Is.EqualTo(expected.SecularVariationHorizontalIntensity).Within(toleranceSecularVariation),
                $"Secular Variation Horizontal Intensity mismatch. Note:  {sourceNote}");
            Assert.That(actual.SecularVariationTotalIntensity,
                Is.EqualTo(expected.SecularVariationTotalIntensity).Within(toleranceSecularVariation),
                $"Secular Variation Total Intensity mismatch. Note:  {sourceNote}");
            Assert.That(actual.SecularVariationNorth,
                Is.EqualTo(expected.SecularVariationNorth).Within(toleranceSecularVariation),
                $"Secular Variation North mismatch. Note:  {sourceNote}");
            Assert.That(actual.SecularVariationEast,
                Is.EqualTo(expected.SecularVariationEast).Within(toleranceSecularVariation),
                $"Secular Variation East mismatch. Note:  {sourceNote}");
            Assert.That(actual.SecularVariationVertical,
                Is.EqualTo(expected.SecularVariationVertical).Within(toleranceSecularVariation),
                $"Secular Variation Vertical mismatch. Note:  {sourceNote}");
        });
    }


    public static IgrfGeomagneticData GetGeomagneticDataFromReferenceModel(IgrfGeomagneticData data,
        IgrfMagneticModelTools.InternalShcModel model)
    {
        return IgrfMagneticModelTools.GetGeomagneticData(data.Latitude, data.Longitude, data.AltitudeInMeters,
            data.Date, model);
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