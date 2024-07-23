using Garmin.Connect.Models;

namespace PointlessWaymarks.SpatialTools;

public interface IRemoteGpxService
{
    string ConnectPassword { get; set; }
    string ConnectUsername { get; set; }

    Task<FileInfo?> DownloadGpxFile(long activityId, string fullNameForFile, CancellationToken cancellationToken,
        IProgress<string>? progress);

    Task<List<GarminActivity>> GetActivityList(DateTime startUtc, DateTime endUtc, IProgress<string>? progress);
}