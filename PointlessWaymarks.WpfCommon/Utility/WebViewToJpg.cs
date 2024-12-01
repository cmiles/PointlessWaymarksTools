using System.IO;
using System.Text.Json;
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
            "document.querySelector('body').style.overflowY='visible'");
        await webContentWebView.CoreWebView2.ExecuteScriptAsync(
            "document.querySelector('body').style.overflow='hidden'");

        var documentHeightArray = await webContentWebView.CoreWebView2.ExecuteScriptAsync(
            """
            [
                document.documentElement.clientHeight,
                document.body ? document.body.scrollHeight : 0,
                document.documentElement.scrollHeight,
                document.body ? document.body.offsetHeight : 0,
                document.documentElement.offsetHeight
            ]
            """);
        var documentHeight =
            JsonSerializer.Deserialize<List<int>>(documentHeightArray)!.Max();

        var documentWidthArray = await webContentWebView.CoreWebView2.ExecuteScriptAsync(
            """
            [
                document.documentElement.clientWidth,
                document.body ? document.body.scrollWidth : 0,
                document.documentElement.scrollWidth,
                document.body ? document.body.offsetWidth : 0,
                document.documentElement.offsetWidth
            ]
            """);
        var documentWidth =
            JsonSerializer.Deserialize<List<int>>(documentWidthArray)!.Max();

        var viewportHeight =
            int.Parse(await webContentWebView.CoreWebView2.ExecuteScriptAsync("visualViewport.height"));
        var viewportWidth = int.Parse(await webContentWebView.CoreWebView2.ExecuteScriptAsync("visualViewport.width"));

        if (documentHeight > viewportHeight)
        {
            await webContentWebView.CoreWebView2.ExecuteScriptAsync("""
                                                                    var x = document.querySelectorAll('*');
                                                                    for(var i=0; i<x.length; i++) {
                                                                        elementStyle = getComputedStyle(x[i]);
                                                                        if(elementStyle.position=="fixed" || elementStyle.position=="sticky") {
                                                                            x[i].style.position="absolute";
                                                                        }
                                                                    }
                                                                    """);
            documentHeight =
                int.Parse(await webContentWebView.CoreWebView2.ExecuteScriptAsync("document.body.scrollHeight"));
        }


        var verticalChunks = (int)Math.Ceiling((double)documentHeight / viewportHeight);
        var verticalImageBytesList = new List<byte[]>();

        var horizontalChunks = (int)Math.Ceiling((double)documentWidth / viewportWidth);

        for (var i = 0; i < verticalChunks; i++)
        {
            using var rowImage = new SKBitmap(documentWidth, viewportHeight);
            using var rowCanvas = new SKCanvas(rowImage);

            var currentWidth = 0;

            for (var j = 0; j < horizontalChunks; j++)
            {
                var scrollToViewFunction = $$"""
                                            window.scrollTo({
                                               top: {{i * viewportHeight}},
                                               left: {{j * viewportWidth}},
                                               behavior: "instant"
                                            });
                                            """;
                await webContentWebView.CoreWebView2.ExecuteScriptAsync(scrollToViewFunction);
                await Task.Delay(1000);
                using var stream = new MemoryStream();
                await webContentWebView.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png,
                    stream);
                var imageBytes = stream.ToArray();

                using var image = SKBitmap.Decode(imageBytes);

                var sourceRect = new SKRect(0, 0, image.Width, image.Height);
                var destRect = new SKRect(currentWidth, 0, currentWidth + image.Width, image.Height);

                if (j == horizontalChunks - 1 && horizontalChunks > 1)
                {
                    var neededLastImageWidth = documentWidth % viewportWidth;
                    destRect = new SKRect(currentWidth - (image.Width - neededLastImageWidth), 0, currentWidth + neededLastImageWidth, image.Height);

                    currentWidth += neededLastImageWidth;
                }
                else
                {
                    currentWidth += image.Width;
                }

                rowCanvas.DrawBitmap(image, sourceRect, destRect);

            }

            using var rowImageEncoded = SKImage.FromBitmap(rowImage);
            using var rowImageStream = new MemoryStream();
            rowImageEncoded.Encode(SKEncodedImageFormat.Png, 100).SaveTo(rowImageStream);
            verticalImageBytesList.Add(rowImageStream.ToArray());
        }

        using var finalImage = new SKBitmap(documentWidth, documentHeight);
        using var canvas = new SKCanvas(finalImage);
        var currentHeight = 0;

        for (var i = 0; i < verticalImageBytesList.Count; i++)
        {
            using var image = SKBitmap.Decode(verticalImageBytesList[i]);
            var sourceRect = new SKRect(0, 0, image.Width, image.Height);
            var destRect = new SKRect(0, currentHeight, image.Width, currentHeight + image.Height);

            if (i == verticalImageBytesList.Count - 1 && verticalImageBytesList.Count > 1)
            {
                var neededLastImageHeight = documentHeight % viewportHeight;
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