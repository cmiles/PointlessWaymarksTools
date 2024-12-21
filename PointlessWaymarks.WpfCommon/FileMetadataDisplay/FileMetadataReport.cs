using System.Diagnostics;
using System.IO;
using System.Text;
using System.Web;
using HtmlTableHelper;
using MetadataExtractor;
using MetadataExtractor.Formats.Xmp;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.WpfCommon.Status;
using XmpCore;

namespace PointlessWaymarks.WpfCommon.FileMetadataDisplay;

public static class FileMetadataReport
{
    public static async Task<string> AllFileMetadataToHtml(FileInfo selectedFile)
    {
        if (selectedFile.Extension.Equals(".xmp", StringComparison.OrdinalIgnoreCase))
        {
            IXmpMeta xmp;
            await using (var stream = File.OpenRead(selectedFile.FullName))
            {
                xmp = XmpMetaFactory.Parse(stream);
            }

            var xmpProperties = xmp.Properties.OrderBy(x => x.Namespace).ThenBy(x => x.Path)
                .Select(x => new { x.Namespace, x.Path, x.Value })
                .ToHtmlTable(new { @class = "pure-table pure-table-striped" });

            var htmlXmpString =
                await xmpProperties.ToHtmlDocumentWithPureCss("Xmp Metadata", "body {margin: 12px;}");

            return htmlXmpString;
        }

        var photoMetaTags = ImageMetadataReader.ReadMetadata(selectedFile.FullName);

        var tagHtml = photoMetaTags.SelectMany(x => x.Tags).OrderBy(x => x.DirectoryName).ThenBy(x => x.Name)
            .ToList().Select(x => new
            {
                DataType = x.Type.ToString(),
                x.DirectoryName,
                Tag = x.Name,
                TagValue = x.Description?.SafeObjectDump()
            }).ToHtmlTable(new { @class = "pure-table pure-table-striped" });

        var xmpDirectory = ImageMetadataReader.ReadMetadata(selectedFile.FullName).OfType<XmpDirectory>()
            .FirstOrDefault();

        var xmpMetadata = xmpDirectory?.GetXmpProperties().Select(x => new { XmpKey = x.Key, XmpValue = x.Value })
            .ToHtmlTable(new { @class = "pure-table pure-table-striped" });

        var htmlStringBuilder = new StringBuilder();

        if (photoMetaTags.SelectMany(x => x.Tags).Any())
        {
            htmlStringBuilder.AppendLine("<h3>Metadata - Part 1</h3><br>");
            htmlStringBuilder.AppendLine(tagHtml);
        }

        if (xmpDirectory != null &&
            xmpDirectory.GetXmpProperties().Select(x => new { XmpKey = x.Key, XmpValue = x.Value }).Any())
        {
            htmlStringBuilder.AppendLine("<br><br><h3>XMP - Part 2</h3><br>");
            htmlStringBuilder.AppendLine(xmpMetadata);
        }

        var htmlString =
            await htmlStringBuilder.ToString().ToHtmlDocumentWithPureCss("Photo Metadata", "body {margin: 12px;}");

        return htmlString;
    }

    public static async Task AllFileMetadataToHtmlDocumentAndOpen(FileInfo? selectedFile,
        StatusControlContext statusContext)
    {
        await ThreadSwitcher.ResumeBackgroundAsync();

        if (selectedFile == null)
        {
            await statusContext.ToastError("No photo...");
            return;
        }

        selectedFile.Refresh();

        if (!selectedFile.Exists)
        {
            await statusContext.ToastError($"File {selectedFile.FullName} doesn't exist?");
            return;
        }

        var metadataHtmlString = await AllFileMetadataToHtml(selectedFile);

        if (string.IsNullOrWhiteSpace(metadataHtmlString))
        {
            await statusContext.ToastError($"No Metadata Found for {selectedFile.FullName}...");
            return;
        }

        var htmlString =
            await
                $"<h1>Metadata Report:</h1><h1>{HttpUtility.HtmlEncode(selectedFile.FullName)}</h1>{metadataHtmlString}"
                    .ToHtmlDocumentWithPureCss("Photo Metadata", "body {margin: 12px;}");

        await ThreadSwitcher.ResumeForegroundAsync();

        var file = new FileInfo(Path.Combine(FileLocationTools.TempStorageDirectory().FullName,
            $"PhotoMetadata-{Path.GetFileNameWithoutExtension(selectedFile.Name)}-{DateTime.Now:yyyy-MM-dd---HH-mm-ss}.htm"));

        await File.WriteAllTextAsync(file.FullName, htmlString);

        var ps = new ProcessStartInfo(file.FullName) { UseShellExecute = true, Verb = "open" };
        Process.Start(ps);
    }
}