using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json.Nodes;
using MetadataExtractor;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.SpatialTools;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.WebViewVirtualDomain;
using PointlessWaymarks.WpfCommon.WpfHtml;
using PointlessWaymarks.WpfCommon.WpfHtmlResources;

namespace PointlessWaymarks.WpfCommon.FileMetadataDisplay;

/// <summary>
///     Interaction logic for FileMetadataDisplayWindow.xaml
/// </summary>
[NotifyPropertyChanged]
public partial class FileMetadataDisplayWindow : IWebViewMessenger
{
    public FileMetadataDisplayWindow(StatusControlContext statusContext,
        string windowTitle)
    {
        InitializeComponent();

        StatusContext = statusContext;

        FromWebView = new WorkQueue<FromWebViewMessage>
        {
            Processor = ProcessFromWebView
        };

        ToWebView = new WorkQueue<ToWebViewRequest>(true);

        DataContext = this;

        WindowTitle = windowTitle;
    }


    public SpatialBounds? MapBounds { get; set; }
    public StatusControlContext StatusContext { get; set; }
    public string WindowTitle { get; set; }
    public WorkQueue<FromWebViewMessage> FromWebView { get; set; }
    public WorkQueue<ToWebViewRequest> ToWebView { get; set; }

    public static async Task<FileMetadataDisplayWindow> CreateInstance(string fileName)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        var factoryStatusContext = new StatusControlContext();

        var metadataWindow = new FileMetadataDisplayWindow(factoryStatusContext, $"Metadata - {fileName}");

        metadataWindow.StatusContext.RunBlockingTask(async () => await metadataWindow.LoadData(fileName));

        return metadataWindow;
    }

    public static FileBuilder FileMetadataMapDocument(string title, MetadataLocation location, string styleBlock = "",
        string javascript = "",
        string serializedMapIcons = "", string bodyContent = "")
    {
        var htmlString = $$$"""
                            <!doctype html>
                            <html lang=en>
                            <head>
                              <meta http-equiv="X-UA-Compatible" content="IE=edge" />
                              <meta charset="utf-8">
                              <meta name="viewport" content="width=device-width, initial-scale=1.0">
                              <title>{{{HtmlEncoder.Default.Encode(title)}}}</title>
                              <link rel="stylesheet" href="https://[[VirtualDomain]]/pure.css" />
                              <link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.3/dist/leaflet.css" integrity="sha256-kLaT2GOSpHechhsozzB+flnD+zUyjE2LlfWPgU04xyI=" crossorigin="" />
                              <link rel="stylesheet" href="https://[[VirtualDomain]]/leaflet.awesome-svg-markers.css">
                              <script src="https://unpkg.com/leaflet@1.9.3/dist/leaflet.js" integrity="sha256-WBkoXOwTeyKclOHuWtc+i2uENFpDZ9YPdf5Hf+D7ewM=" crossorigin=""></script>
                              <script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
                              <script src="https://[[VirtualDomain]]/leaflet.awesome-svg-markers.js"></script>
                              <script src="https://[[VirtualDomain]]/leafletBingLayer.js"></script>
                              <script src="https://[[VirtualDomain]]/localMapCommon.js"></script>
                                {{{(string.IsNullOrWhiteSpace(styleBlock) ? string.Empty : """<link rel="stylesheet" href="https://[[VirtualDomain]]/customStyle.css" />""")}}}
                                {{{(string.IsNullOrWhiteSpace(javascript) ? string.Empty : """<script src="https://[[VirtualDomain]]/customScript.js"></script>""")}}}
                            </head>
                            <body onload="initialDocumentLoad();">
                                 <h3 style="text-align: center;">{{{HtmlEncoder.Default.Encode(title)}}}</h3>
                                 <div id="mainMap" class="leaflet-container leaflet-retina leaflet-fade-anim leaflet-grab leaflet-touch-drag"
                                    style="height: 70vh;"></div>
                                 <div style="text-align: center; margin-top: 20px;">
                                    <a href="https://www.peakfinder.org/?lat={{{location.Latitude}}}&lng={{{location.Longitude}}}{{{(location.PhotoDirection is null ? "" : $"&azi={location.PhotoDirection.Value:F0}")}}}" target="_blank" class="pure-button pure-button-primary">Open in Peakfinder Web</a>
                                    <a href="https://www.google.com/maps/search/?api=1&query={{{location.Latitude}}},{{{location.Longitude}}}" target="_blank" class="pure-button pure-button-primary">Google Maps</a>
                                    <a href="http://www.openstreetmap.org/?mlat={{{location.Latitude}}}&mlon={{{location.Longitude}}}&zoom=13&layers=C" target="_blank" class="pure-button pure-button-primary">OpenStreetMap</a>
                                </div>
                                 {{{bodyContent}}}
                            </body>
                            </html>
                            """;

        var initialWebFilesMessage = new FileBuilder();

        if (!string.IsNullOrWhiteSpace(styleBlock))
            initialWebFilesMessage.Create.Add(new FileBuilderCreate("customStyle.css", styleBlock));
        if (!string.IsNullOrWhiteSpace(javascript))
            initialWebFilesMessage.Create.Add(new FileBuilderCreate("customScript.js", javascript));
        initialWebFilesMessage.Create.Add(new FileBuilderCreate("leafletBingLayer.js",
            WpfHtmlResourcesHelper.LeafletBingLayerJs()));
        initialWebFilesMessage.Create.Add(new FileBuilderCreate("localMapCommon.js",
            WpfHtmlResourcesHelper.LocalMapCommonJs()));
        initialWebFilesMessage.Create.AddRange(WpfHtmlResourcesHelper.AwesomeMapSvgMarkers());
        if (!string.IsNullOrWhiteSpace(serializedMapIcons))
            initialWebFilesMessage.Create.Add(new FileBuilderCreate("pwMapSvgIcons.json", serializedMapIcons));
        initialWebFilesMessage.Create.Add(new FileBuilderCreate("Index.html", htmlString, true));

        return initialWebFilesMessage;
    }

    public async Task LoadData(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            await this.SetupDocumentWithMinimalCss(
                """<h3 style="text-align:center;">Error - Blank or Empty Filename?</h3>""", "File Metadata Report");
            return;
        }

        if (!File.Exists(fileName))
        {
            await this.SetupDocumentWithMinimalCss(
                $"""<h3 style="text-align:center;">Error - File '{fileName}' does not exist or could not be accessed?</h3>""",
                "File Metadata Report");
            return;
        }

        var fileMetadataHtml = await FileMetadataReport.AllFileMetadataToHtml(new FileInfo(fileName));
        var metadataDirectories = ImageMetadataReader.ReadMetadata(fileName);
        var createdOn = await FileMetadataEmbeddedTools.CreatedOnLocalAndUtc(metadataDirectories);
        var location = await FileMetadataEmbeddedTools.LocationFromExif(metadataDirectories, true,
            createdOn.createdOnUtc ?? createdOn.createdOnLocal ?? DateTime.Now, StatusContext.ProgressTracker());

        if (location.HasValidLocation())
        {
            var initialWebFilesMessage =
                FileMetadataMapDocument($"Metadata: {fileName}", location, bodyContent: fileMetadataHtml);

            var startingPoint = PointTools.Wgs84Point(location.Longitude!.Value, location.Latitude!.Value, 0);

            var featureCollection = new FeatureCollection
            {
                new Feature(startingPoint,
                    new AttributesTable(new Dictionary<string, object>()))
            };

            if (location.PhotoDirection is not null)
            {
                var endingPoint = PointTools.ProjectCoordinate(startingPoint, location.PhotoDirection.Value, 100000);
                var line = new LineString([startingPoint.Coordinate, endingPoint.Coordinate]);

                featureCollection.Add(new Feature(line, new AttributesTable(new Dictionary<string, object>())));
            }

            var bounds = SpatialBounds.FromCoordinates(location.Latitude.Value, location.Longitude.Value, 0)
                .ExpandToMinimumMeters(3000);
            var mapJson = await MapJson.NewMapFeatureCollectionDtoSerialized([featureCollection], bounds);

            ToWebView.Enqueue(initialWebFilesMessage);

            ToWebView.Enqueue(NavigateTo.CreateRequest("Index.html", true));

            ToWebView.Enqueue(ExecuteJavaScript.CreateRequest(
                $"initialMapLoad({location.Latitude.Value}, {location.Longitude.Value})",
                true));

            ToWebView.Enqueue(new JsonData
            {
                Json = mapJson
            });
        }
        else
        {
            await this.SetupDocumentWithMinimalCss(fileMetadataHtml, "File Metadata Report");
        }
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