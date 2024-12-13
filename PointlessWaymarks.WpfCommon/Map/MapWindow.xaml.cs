using System.Text.Json.Nodes;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.SpatialTools;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.WebViewVirtualDomain;
using PointlessWaymarks.WpfCommon.WpfHtml;

namespace PointlessWaymarks.WpfCommon.Map;

[NotifyPropertyChanged]
public partial class MapWindow : IWebViewMessenger
{
    public MapWindow(StatusControlContext statusContext, double initialLatitude, double initialLongitude,
        string windowTitle)
    {
        InitializeComponent();

        StatusContext = statusContext;

        FromWebView = new WorkQueue<FromWebViewMessage>
        {
            Processor = ProcessFromWebView
        };

        ToWebView = new WorkQueue<ToWebViewRequest>(true);

        this.SetupCmsLeafletMapHtmlAndJs("Map", initialLatitude, initialLongitude, false);

        DataContext = this;
    }

    public SpatialBounds? MapBounds { get; set; }
    public StatusControlContext StatusContext { get; set; }

    public string WindowTitle { get; set; } = "Map Window";

    public WorkQueue<FromWebViewMessage> FromWebView { get; set; }
    public WorkQueue<ToWebViewRequest> ToWebView { get; set; }

    public static async Task<MapWindow> CreateInstance(double? initialLatitude, double? initialLongitude,
        string windowTitle)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var factoryStatusContext = new StatusControlContext();

        var mapWindow = new MapWindow(factoryStatusContext, initialLatitude ?? 32.247222,
            initialLongitude ?? -110.467222, windowTitle);

        return mapWindow;
    }

    private async Task MapMessageReceived(string mapMessage)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        var parsedJson = JsonNode.Parse(mapMessage);

        if (parsedJson == null) return;

        var messageType = parsedJson["messageType"]?.ToString() ?? string.Empty;

        if (messageType == "mapBoundsChange")
            try
            {
                MapBounds = new SpatialBounds(parsedJson["bounds"]["_northEast"]["lat"].GetValue<double>(),
                    parsedJson["bounds"]["_northEast"]["lng"].GetValue<double>(),
                    parsedJson["bounds"]["_southWest"]["lat"].GetValue<double>(),
                    parsedJson["bounds"]["_southWest"]["lng"].GetValue<double>());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
    }

    public Task ProcessFromWebView(FromWebViewMessage args)
    {
        if (!string.IsNullOrWhiteSpace(args.Message))
            StatusContext.RunFireAndForgetNonBlockingTask(async () => await MapMessageReceived(args.Message));
        return Task.CompletedTask;
    }

    public async Task ShowMarker(double markerLatitude, double markerLongitude)
    {
        var featureCollection = new FeatureCollection
        {
            new Feature(
                PointTools.Wgs84Point(markerLongitude, markerLatitude, 0),
                new AttributesTable(new Dictionary<string, object>()))
        };

        var bounds = SpatialBounds.FromCoordinates(markerLatitude, markerLongitude, 0).ExpandToMinimumMeters(1000);
        var mapJson = await MapJson.NewMapFeatureCollectionDtoSerialized([featureCollection], bounds);

        ToWebView.Enqueue(new JsonData
        {
            Json = mapJson
        });
    }

    public async Task ShowMarkerAndBearing(double markerLatitude, double markerLongitude, double bearing,
        double distanceInMeters)
    {
        var startingPoint = PointTools.Wgs84Point(markerLongitude, markerLatitude, 0);
        var endingPoint = PointTools.ProjectCoordinate(startingPoint, bearing, distanceInMeters);
        var line = new LineString([startingPoint.Coordinate, endingPoint.Coordinate]);

        var featureCollection = new FeatureCollection
        {
            new Feature(startingPoint,
                new AttributesTable(new Dictionary<string, object>())),
            new Feature(line, new AttributesTable(new Dictionary<string, object>()))
        };

        var bounds = SpatialBounds.FromCoordinates(markerLatitude, markerLongitude, 0)
            .ExpandToMinimumMeters(Math.Min(3000, distanceInMeters));
        var mapJson = await MapJson.NewMapFeatureCollectionDtoSerialized([featureCollection], bounds);

        ToWebView.Enqueue(new JsonData
        {
            Json = mapJson
        });
    }
}