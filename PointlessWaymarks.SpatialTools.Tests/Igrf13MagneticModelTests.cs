namespace PointlessWaymarks.SpatialTools.Tests;

/// <summary>
///     Tests some locations against values calculated from IGRF-13 magnetic model
///     via [The IGRF Model (13th Generation)](https://geomag.bgs.ac.uk/data_service/models_compass/igrf_calc.html)
/// </summary>
public class Igrf13MagneticModelTests
{
    public IgrfGeomagneticData BadwaterBasin = new()
    {
        Date = new DateOnly(1949, 1, 1),
        Latitude = 36.250278,
        Longitude = -116.825833,
        AltitudeInMeters = -90,
        Declination = 16.149,
        Inclination = 61.958,
        HorizontalIntensity = 24651,
        TotalIntensity = 52435,
        NorthComponent = 23678,
        EastComponent = 6856,
        VerticalComponent = 46279,
        SecularVariationDeclination = -2.5,
        SecularVariationInclination = -1.0,
        SecularVariationHorizontalIntensity = -3.5,
        SecularVariationTotalIntensity = -37.4,
        SecularVariationNorth = 1.6,
        SecularVariationEast = -18.1,
        SecularVariationVertical = -40.5
    };

    public IgrfGeomagneticData Lohtse = new()
    {
        Date = new DateOnly(1980, 1, 1),
        Latitude = 27.9617,
        Longitude = 86.9333,
        AltitudeInMeters = 8520,
        Declination = -0.302,
        Inclination = 40.926,
        HorizontalIntensity = 35960,
        TotalIntensity = 47595,
        NorthComponent = 35960,
        EastComponent = -190,
        VerticalComponent = 31179,
        SecularVariationDeclination = 0.3,
        SecularVariationInclination = 4.7,
        SecularVariationHorizontalIntensity = -48.9,
        SecularVariationTotalIntensity = -8.0,
        SecularVariationNorth = -48.9,
        SecularVariationEast = 3.3,
        SecularVariationVertical = 44.2
    };

    public IgrfGeomagneticData MountKosciuszko = new()
    {
        Date = new DateOnly(1990, 1, 1),
        Latitude = -36.455833,
        Longitude = 148.263611,
        AltitudeInMeters = 2230,
        Declination = 12.382,
        Inclination = -67.395,
        HorizontalIntensity = 22823,
        TotalIntensity = 59375,
        NorthComponent = 22292,
        EastComponent = 4894,
        VerticalComponent = -54813,
        SecularVariationDeclination = 1.2,
        SecularVariationInclination = 0.1,
        SecularVariationHorizontalIntensity = -5.0,
        SecularVariationTotalIntensity = -16.1,
        SecularVariationNorth = -6.5,
        SecularVariationEast = 6.5,
        SecularVariationVertical = 15.3
    };

    //This data is set up for the Igrf13 model and will fail if you run it against the
    //Igrf14 - if you get a failure double-check the correct model is being used. The 
    //other named data in this class will be within tolerance for the Igrf14 model.
    public IgrfGeomagneticData MountStHelens = new()
    {
        Date = new DateOnly(2015, 8, 3),
        Latitude = 46.1912,
        Longitude = -122.1944,
        AltitudeInMeters = 2550,
        Declination = 15.675,
        Inclination = 68.074,
        HorizontalIntensity = 19814,
        TotalIntensity = 53063,
        NorthComponent = 19077,
        EastComponent = 5354,
        VerticalComponent = 49224,
        SecularVariationDeclination = -6.0,
        SecularVariationInclination = -1.6,
        SecularVariationHorizontalIntensity = -17.9,
        SecularVariationTotalIntensity = -108.0,
        SecularVariationNorth = -7.8,
        SecularVariationEast = -38.2,
        SecularVariationVertical = -109.2
    };

    public IgrfGeomagneticData RinconPeak = new()
    {
        Date = new DateOnly(2001, 1, 1),
        Latitude = 32.119743,
        Longitude = -110.523021,
        AltitudeInMeters = 2580,
        Declination = 11.309,
        Inclination = 58.877,
        HorizontalIntensity = 25084,
        TotalIntensity = 48531,
        NorthComponent = 24597,
        EastComponent = 4919,
        VerticalComponent = 41546,
        SecularVariationDeclination = -5.5,
        SecularVariationInclination = -0.7,
        SecularVariationHorizontalIntensity = -37.1,
        SecularVariationTotalIntensity = -88.9,
        SecularVariationNorth = -28.5,
        SecularVariationEast = -46.8,
        SecularVariationVertical = -81.5
    };

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

    [Test]
    public void BadwaterBasinTest()
    {
        var testResult = GetGeomagneticDataFromReferenceModel(BadwaterBasin);
        AssertGeomagneticDataEqual(BadwaterBasin, testResult);
    }

    public static IgrfGeomagneticData GetGeomagneticDataFromReferenceModel(IgrfGeomagneticData data)
    {
        return IgrfMagneticModelTools.GetGeomagneticData(data.Latitude, data.Longitude, data.AltitudeInMeters,
            data.Date, IgrfMagneticModelTools.InternalShcModel.Igrf13Model);
    }

    [Test]
    public void LohtseTest()
    {
        var testResult = GetGeomagneticDataFromReferenceModel(Lohtse);
        AssertGeomagneticDataEqual(Lohtse, testResult);
    }

    [Test]
    public void MountKosciuszkoTest()
    {
        var testResult = GetGeomagneticDataFromReferenceModel(MountKosciuszko);
        AssertGeomagneticDataEqual(MountKosciuszko, testResult);
    }

    [Test]
    public void MountStHelensTest()
    {
        var testResult = GetGeomagneticDataFromReferenceModel(MountStHelens);
        AssertGeomagneticDataEqual(MountStHelens, testResult);
    }

    [Test]
    public void RinconPeakTest()
    {
        var testResult = GetGeomagneticDataFromReferenceModel(RinconPeak);
        AssertGeomagneticDataEqual(RinconPeak, testResult);
    }

    [Test]
    [Repeat(20)]
    public async Task TestIgrf2013AutomatedApi()
    {
        var (latitude, longitude, date, elevation) = Igrf2013AutomatedApi.GetRandomLatLongDate();
        var automatedResult =
            await Igrf2013AutomatedApi.GetIgrfMagneticDataAsync(latitude, longitude, elevation, date);
        var calculatedResult = GetGeomagneticDataFromReferenceModel(automatedResult.result);
        AssertGeomagneticDataEqual(automatedResult.result, calculatedResult,
            $"{latitude} {longitude} - {date} - {elevation}m. {automatedResult.url}");

        await Task.Delay(5000);
    }
}