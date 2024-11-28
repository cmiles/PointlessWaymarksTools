using System.IO;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using OneOf;
using OneOf.Types;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.WpfCommon.Status;
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

        var viewPortUserPositionString =
            await webContentWebView.CoreWebView2.ExecuteScriptAsync("visualViewport.pageTop");
        var viewPortUserPosition = int.Parse(viewPortUserPositionString);

        await webContentWebView.CoreWebView2.ExecuteScriptAsync(
            "document.querySelector('body').style.overflow='hidden'");

        await webContentWebView.CoreWebView2.ExecuteScriptAsync("""
                                                                var x = document.querySelectorAll('*');
                                                                for(var i=0; i<x.length; i++) {
                                                                    elementStyle = getComputedStyle(x[i]);
                                                                    if(elementStyle.position=="fixed" || elementStyle.position=="sticky") {
                                                                        x[i].style.position="absolute";
                                                                    }
                                                                }
                                                                """);

        var imageViewportHeight =
            int.Parse(await webContentWebView.CoreWebView2.ExecuteScriptAsync("visualViewport.height"));
        var viewportWidth = int.Parse(await webContentWebView.CoreWebView2.ExecuteScriptAsync("visualViewport.width"));
        var documentScrollHeight =
            int.Parse(await webContentWebView.CoreWebView2.ExecuteScriptAsync("document.body.scrollHeight"));

        var chunks = (int)Math.Ceiling((double)documentScrollHeight / imageViewportHeight);
        var imageBytesList = new List<byte[]>();

        for (var i = 0; i < chunks; i++)
        {
            var scrollToTopFunction = $$"""
                                        window.scrollTo({
                                           top: {{i * imageViewportHeight}},
                                           left: 0,
                                           behavior: "instant"
                                        });
                                        """;

            await webContentWebView.CoreWebView2.ExecuteScriptAsync(scrollToTopFunction);
            await Task.Delay(1000);

            using var stream = new MemoryStream();
            await webContentWebView.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, stream);
            var imageBytes = stream.ToArray();
            imageBytesList.Add(imageBytes);
        }

        using var finalImage = new SKBitmap(viewportWidth, documentScrollHeight);
        using var canvas = new SKCanvas(finalImage);
        var currentHeight = 0;

        for (var i = 0; i < imageBytesList.Count; i++)
        {
            using var image = SKBitmap.Decode(imageBytesList[i]);
            var sourceRect = new SKRect(0, 0, image.Width, image.Height);
            var destRect = new SKRect(0, currentHeight, image.Width, currentHeight + image.Height);

            if (i == imageBytesList.Count - 1)
            {
                var neededLastImageHeight = documentScrollHeight % imageViewportHeight;
                destRect = new SKRect(0, currentHeight - (image.Height - neededLastImageHeight), image.Width,
                    currentHeight + neededLastImageHeight);

                currentHeight += neededLastImageHeight;
            }
            else
            {
                currentHeight += image.Height;
            }

            canvas.DrawBitmap(image, sourceRect, destRect);
        }

        using var imageStream = new MemoryStream();
        using var finalImageEncoded = SKImage.FromBitmap(finalImage);
        finalImageEncoded.Encode(SKEncodedImageFormat.Jpeg, 100).SaveTo(imageStream);
        var finalImageBytes = imageStream.ToArray();

        await ThreadSwitcher.ResumeForegroundAsync();
        webContentWebView.Reload();

        var scrollBackToUserPositionFunction = $$"""
                                                 window.scrollTo({
                                                    top: {{viewPortUserPosition}},
                                                    left: 0,
                                                    behavior: "instant"
                                                 });
                                                 """;

        await webContentWebView.CoreWebView2.ExecuteScriptAsync(scrollBackToUserPositionFunction);

        return new Success<byte[]>(finalImageBytes);
    }
}