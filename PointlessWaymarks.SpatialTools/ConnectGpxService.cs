using Garmin.Connect;
using Garmin.Connect.Auth;
using Garmin.Connect.Models;
using Polly;

namespace PointlessWaymarks.SpatialTools;

public class ConnectGpxService : IRemoteGpxService
{
    private GarminConnectClient? _client;
    public required string ConnectPassword { get; set; }
    public required string ConnectUsername { get; set; }

    public async Task<FileInfo?> DownloadGpxFile(long activityId, string fullNameForFile,
        CancellationToken cancellationToken, IProgress<string>? progress)
    {
        var client = _client ?? new GarminConnectClient(new GarminConnectContext(
            new HttpClient { Timeout = TimeSpan.FromSeconds(10) },
            new BasicAuthParameters(ConnectUsername, ConnectPassword)));

        var file = await Policy.Handle<Exception>(e => e is not TaskCanceledException).WaitAndRetryAsync(3,
                i => TimeSpan.FromSeconds(2 * i),
                (exception, sleepDuration, retryCount, _) =>
                {
                    progress?.Report(exception.Message);
                    progress?.Report(
                        $"Garmin Connect Download Activity Failure - Retry Count: {retryCount}, Current Wait Seconds: {sleepDuration.TotalSeconds} ");
                })
            .ExecuteAsync(async () =>
            {
                progress?.Report($"Downloading Activity Id {activityId}");
                cancellationToken.ThrowIfCancellationRequested();
                return await client.DownloadActivity(activityId, ActivityDownloadFormat.GPX, cancellationToken);
            });

        if (file is null) return null;

        cancellationToken.ThrowIfCancellationRequested();

        progress?.Report($"Writing GPX File {fullNameForFile} (Activity Id {activityId})");
        await File.WriteAllBytesAsync(fullNameForFile, file, cancellationToken);

        return new FileInfo(fullNameForFile);
    }

    public async Task<List<GarminActivity>> GetActivityList(DateTime startUtc, DateTime endUtc,
        IProgress<string>? progress)
    {
        var client = _client ?? new GarminConnectClient(new GarminConnectContext(
            new HttpClient { Timeout = TimeSpan.FromSeconds(10) }, new
                BasicAuthParameters(ConnectUsername, ConnectPassword)));
        var activities = await Policy.Handle<Exception>(e => e is not TaskCanceledException).WaitAndRetryAsync(3,
                i => TimeSpan.FromSeconds(2 * i),
                (exception, sleepDuration, retryCount, _) =>
                {
                    progress?.Report(exception.Message);
                    progress?.Report(
                        $"Garmin Connect Activity List Download Failure - Retry Count: {retryCount}, Current Wait Seconds: {sleepDuration.TotalSeconds} ");
                })
            .ExecuteAsync(async () =>
            {
                progress?.Report($"Downloading Activities from {startUtc} to {endUtc}");
                return await client.GetActivitiesByDate(startUtc, endUtc, string.Empty) ?? [];
            });
        return activities.ToList();
    }
}