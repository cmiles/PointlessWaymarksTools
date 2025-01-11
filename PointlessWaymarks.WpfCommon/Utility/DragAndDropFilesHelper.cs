using System.IO;
using System.Windows;
using GongSolutions.Wpf.DragDrop;
using PointlessWaymarks.CmsWpfControls.Utility;
using PointlessWaymarks.CommonTools;

namespace PointlessWaymarks.WpfCommon.Utility;

public static class DragAndDropFilesHelper
{
    /// <summary>
    ///     Returns a list of file names, without Paths, from a Drag and Drop event. This is useful in a scenario
    ///     in a DragOver where you are only going to accept certain filenames or extensions - no paths are given
    ///     because these files may be virtual files without a traditional path. DOES NOT handle directories
    ///     in any way. This method handles Virtual Files - but not Virtual Directories.
    /// </summary>
    /// <param name="dropInfo"></param>
    /// <param name="fileExtensionFilter"></param>
    /// <returns></returns>
    public static List<string> DroppedFileNames(IDropInfo dropInfo, List<string>? fileExtensionFilter = null)
    {
        fileExtensionFilter ??= [];

        if (dropInfo.Data is not IDataObject systemDataObject) return [];

        if (systemDataObject.GetDataPresent(DataFormats.FileDrop))
        {
            if (systemDataObject.GetData(DataFormats.FileDrop) is not string[] fileDropList ||
                !fileDropList.Any()) return [];

            var fileDropFileNamesList = fileDropList.Select(Path.GetFileName).Where(x => !string.IsNullOrWhiteSpace(x))
                .Cast<string>()
                .ToList();

            return fileExtensionFilter.Any()
                ? fileDropFileNamesList.Where(x =>
                    fileExtensionFilter.Contains(Path.GetExtension(x), StringComparer.OrdinalIgnoreCase)).ToList()
                : fileDropFileNamesList;
        }

        if (!systemDataObject.GetDataPresent("FileGroupDescriptorW") ||
            !systemDataObject.GetDataPresent("FileContents"))
            return [];

        var virtualFileDescriptor = (MemoryStream)systemDataObject.GetData("FileGroupDescriptorW")!;
        var virtualFileList = VirtualFileClipboardHelper.ReadFileDescriptor(virtualFileDescriptor)
            .Select(x => Path.GetFileName(x.FileName)).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

        return fileExtensionFilter.Any()
            ? virtualFileList
                .Where(x => fileExtensionFilter.Contains(Path.GetExtension(x),
                    StringComparer.OrdinalIgnoreCase)).ToList()
            : virtualFileList;
    }

    /// <summary>
    ///     Returns a list of files, including paths, from IDropInfo. A temporary directory is required
    ///     because Virtual Files will be written to disk in the temporary directory (note that if using
    ///     DroppedFileNames to filter files in a DragOver that file names may not be the same when
    ///     written to the temp directory to ensure names are valid and unique). Do not use this method
    ///     unless you want Virtual Files to be written to the file system. DOES NOT handle directories
    ///     in any way. This method handles Virtual Files - but not Virtual Directories.
    /// </summary>
    /// <param name="dropInfo"></param>
    /// <param name="temporaryDirectory"></param>
    /// <param name="fileExtensionFilter"></param>
    /// <returns></returns>
    public static List<string> DroppedFiles(IDropInfo dropInfo, DirectoryInfo temporaryDirectory,
        List<string>? fileExtensionFilter = null)
    {
        fileExtensionFilter ??= [];

        if (dropInfo.Data is not IDataObject systemDataObject) return [];

        if (systemDataObject.GetDataPresent(DataFormats.FileDrop))
        {
            if (systemDataObject.GetData(DataFormats.FileDrop) is not string[] dropFileList ||
                !dropFileList.Any()) return [];

            return fileExtensionFilter.Any()
                ? dropFileList.Where(x =>
                    fileExtensionFilter.Contains(Path.GetExtension(x), StringComparer.OrdinalIgnoreCase)).ToList()
                : dropFileList.ToList();
        }

        if (!systemDataObject.GetDataPresent("FileGroupDescriptorW") ||
            !systemDataObject.GetDataPresent("FileContents"))
            return [];

        var virtualFileDescriptor = (MemoryStream)systemDataObject.GetData("FileGroupDescriptorW")!;

        var virtualFiles = VirtualFileClipboardHelper.ReadFileDescriptor(virtualFileDescriptor);
        var virtualFileIndex = 0;

        var temporaryFiles = new List<string>();

        foreach (var virtualFile in virtualFiles)
        {
            if ((virtualFile.FileAttributes & FileAttributes.Directory) != 0)
            {
                continue;
            }
            else
            {
                if (fileExtensionFilter.Any() &&
                    !fileExtensionFilter.Contains(Path.GetExtension(virtualFile.FileName),
                        StringComparer.OrdinalIgnoreCase)) continue;

                var virtualFileData = VirtualFileClipboardHelper.GetFileContents(systemDataObject, virtualFileIndex);

                if (virtualFileData is null) return [];

                virtualFileData.Position = 0;
                var filePath = UniqueFileTools.UniqueFile(temporaryDirectory, virtualFile.FileName)!;

                temporaryFiles.Add(filePath.FullName);
                using var temporaryFileStream = new FileStream(filePath.FullName, FileMode.Create, FileAccess.Write);
                virtualFileData.CopyTo(temporaryFileStream);
            }

            virtualFileIndex++;
        }

        return temporaryFiles;
    }
}