namespace PointlessWaymarks.SpatialTools.Tests;

public class IgrfAutomatedApiTests
{
    [Test]
    [Repeat(20)]
    public async Task TestIgrf2014AutomatedApi()
    {
        var (latitude, longitude, date, elevation) = IgrfTestHelpers.GetRandomLatLongDate();
        var automatedResult =
            await IgrfGeomagneticApi.GetIgrfMagneticDataAsync(latitude, longitude, elevation, date);
        var calculatedResult = IgrfTestHelpers.GetGeomagneticDataFromReferenceModel(automatedResult.result,
            IgrfMagneticModelTools.InternalShcModel.Igrf14Model);
        IgrfTestHelpers.AssertGeomagneticDataEqual(automatedResult.result, calculatedResult,
            $"{latitude} {longitude} - {date} - {elevation}m. {automatedResult.url}");

        await Task.Delay(5000);
    }
}