using System.IO;
using System.Text.Json;
using Microsoft.Web.WebView2.Wpf;
using OneOf;
using OneOf.Types;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.WpfCommon.Status;
using Polly;
using Polly.Timeout;
using SkiaSharp;

namespace PointlessWaymarks.WpfCommon.Utility;

public static class WebViewToJpg
{
    public static async Task<string?> SaveByteArrayAsJpg(byte[]? byteArray, string suggestedStartingDirectory,
        StatusControlContext statusContext)
    {
        if (byteArray is null) return null;

        var saveDialog = new VistaSaveFileDialog { Filter = "jpg files (*.jpg;*.jpeg)|*.jpg;*.jpeg" };

        var suggestedDirectoryIsValid = !string.IsNullOrWhiteSpace(suggestedStartingDirectory) &&
                                        Directory.Exists(suggestedStartingDirectory);

        if (suggestedDirectoryIsValid)
            saveDialog.FileName = $"{suggestedStartingDirectory}\\";

        if (!saveDialog.ShowDialog() ?? true) return null;

        var newFilename = saveDialog.FileName;

        if (!(Path.GetExtension(newFilename).Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
              Path.GetExtension(newFilename).Equals(".jpeg", StringComparison.OrdinalIgnoreCase)))
            newFilename += ".jpg";

        await ThreadSwitcher.ResumeBackgroundAsync();

        statusContext.Progress($"Writing {byteArray.Length} image bytes");

        await File.WriteAllBytesAsync(newFilename, byteArray);

        await statusContext.ToastSuccess($"Screenshot saved to {newFilename}");

        return newFilename;
    }

    public static async Task<OneOf<Success<byte[]>, Error<string>>> SaveCurrentPageAsJpeg(WebView2 webContentWebView)
    {
        await ThreadSwitcher.ResumeForegroundAsync();

        await ThreadSwitcher.ResumeForegroundAsync();

        var viewPortUserPositionString =
            await webContentWebView.CoreWebView2.ExecuteScriptAsync("visualViewport.pageTop");
        var viewPortUserPosition = int.Parse(viewPortUserPositionString);

        var overflowUserSetting =
            await webContentWebView.CoreWebView2.ExecuteScriptAsync("document.querySelector('body').style.overflow");
        if (string.IsNullOrWhiteSpace(overflowUserSetting) || overflowUserSetting.Equals("\"\"")) overflowUserSetting = string.Empty;

        await webContentWebView.CoreWebView2.ExecuteScriptAsync(
            "document.querySelector('body').style.overflow='hidden'");

        var imageViewportHeight = int.Parse(await webContentWebView.CoreWebView2.ExecuteScriptAsync("visualViewport.height"));
        var viewportWidth = int.Parse(await webContentWebView.CoreWebView2.ExecuteScriptAsync("visualViewport.width"));
        var documentScrollHeight =
            int.Parse(await webContentWebView.CoreWebView2.ExecuteScriptAsync("document.body.scrollHeight"));

        var overlap = 24;
        var chunks = (int)Math.Ceiling((double)(documentScrollHeight - overlap) / (imageViewportHeight - overlap));
        var imageBytesList = new List<byte[]>();

        for (var i = 0; i < chunks; i++)
        {
            var scrollToTopFunction = $$"""
                                        window.scrollTo({
                                           top: {{i * (imageViewportHeight - overlap)}},
                                           left: 0,
                                           behavior: "instant"
                                        });
                                        """;

            await webContentWebView.CoreWebView2.ExecuteScriptAsync(scrollToTopFunction);
            await Task.Delay(1000);

            string screenshotParamsJson;

            if (i == 0)
            {
                var settings = new
                {
                    format = "jpeg",
                    fromSurface = true,
                    captureBeyondViewport = true
                };
                screenshotParamsJson = JsonSerializer.Serialize(settings);
            }
            else
            {
                var clip = new
                {
                    x = 0,
                    y = i * (imageViewportHeight - overlap),
                    width = viewportWidth + 40,
                    height = Math.Min(imageViewportHeight, documentScrollHeight - i * (imageViewportHeight - overlap)),
                    scale = 1
                };

                var settings = new
                {
                    format = "jpeg",
                    clip,
                    fromSurface = true,
                    captureBeyondViewport = true
                };
                screenshotParamsJson = JsonSerializer.Serialize(settings);
            }

            await ThreadSwitcher.ResumeForegroundAsync();

            string imageResultsJson;

            try
            {
                var jpegScreenShotTimeoutPolicy = Policy.TimeoutAsync(6, TimeoutStrategy.Pessimistic);

                imageResultsJson = await jpegScreenShotTimeoutPolicy.ExecuteAsync(async () =>
                    await webContentWebView.CoreWebView2.CallDevToolsProtocolMethodAsync("Page.captureScreenshot",
                        screenshotParamsJson));
            }
            catch (TimeoutRejectedException)
            {
                return new Error<string>("Failed to capture screenshot - operation timed out.");
            }

            string imageResults;

            using (var doc = JsonDocument.Parse(imageResultsJson))
            {
                if (doc.RootElement.TryGetProperty("data", out var dataElement))
                {
                    imageResults = dataElement.GetString() ?? string.Empty;

                    if (string.IsNullOrEmpty(imageResults))
                        return new Error<string>("Failed to capture screenshot - empty data.");
                }
                else
                {
                    return new Error<string>("Failed to capture screenshot - data json not found.");
                }
            }

            var imageBytes = Convert.FromBase64String(imageResults);
            imageBytesList.Add(imageBytes);
        }

        await webContentWebView.CoreWebView2.ExecuteScriptAsync(
            $"document.querySelector('body').style.overflow='{overflowUserSetting}'");

        var scrollBackToUserPositionFunction = $$"""
                                                 window.scrollTo({
                                                    top: {{viewPortUserPosition}},
                                                    left: 0,
                                                    behavior: "instant"
                                                 });
                                                 """;

        await webContentWebView.CoreWebView2.ExecuteScriptAsync(scrollBackToUserPositionFunction);

        using var finalImage = new SKBitmap(viewportWidth, documentScrollHeight);
        using var canvas = new SKCanvas(finalImage);
        var currentHeight = 0;

        foreach (var imageBytes in imageBytesList)
        {
            using var image = SKBitmap.Decode(imageBytes);
            var sourceRect = new SKRect(0, 0, image.Width, image.Height);
            var destRect = new SKRect(0, currentHeight, image.Width, currentHeight + image.Height);

            if (currentHeight > 0)
            {
                sourceRect.Top += overlap;
                destRect.Top += overlap;
            }

            canvas.DrawBitmap(image, sourceRect, destRect);
            currentHeight += image.Height - overlap;
        }

        using var imageStream = new MemoryStream();
        using var finalImageEncoded = SKImage.FromBitmap(finalImage);
        finalImageEncoded.Encode(SKEncodedImageFormat.Jpeg, 100).SaveTo(imageStream);
        var finalImageBytes = imageStream.ToArray();

        return new Success<byte[]>(finalImageBytes);
    }
}