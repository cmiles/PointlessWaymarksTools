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
    ///     because these files may be virtual files without a traditional path. DOES NOT handle Virtual directories
    ///     in any way - only virtual files - so the behavior will be inconsistent based on the source (2025/1/12 -
    ///     I am not sure how to work with Virtual Directories).
    /// </summary>
    /// <param name="dropInfo"></param>
    /// <param name="topLevelFolderFiles">If a FileDrop DataFormat is found take the top level files from any directory.</param>
    /// <param name="fileExtensionFilter"></param>
    /// <returns></returns>
    public static List<string> DroppedFileNames(IDropInfo dropInfo, bool topLevelFolderFiles = false,
        List<string>? fileExtensionFilter = null)
    {
        fileExtensionFilter ??= [];

        if (dropInfo.Data is not IDataObject systemDataObject) return [];

        if (systemDataObject.GetDataPresent(DataFormats.FileDrop))
        {
            if (systemDataObject.GetData(DataFormats.FileDrop) is not string[] dropFileList ||
                !dropFileList.Any()) return [];

            //2025-01-12: The top level files only is the functionality that is currently needed - I'm also leary of how to handle a drop
            //of a massive directory...
            var selectedDirectoryFiles = topLevelFolderFiles
                ? dropFileList.Where(Directory.Exists).Select(x => new DirectoryInfo(x))
                    .SelectMany(x => x.GetFiles("*.*", SearchOption.TopDirectoryOnly)).OrderBy(x => x.FullName).ToList()
                : [];

            var fileDropFileNamesList = dropFileList.Where(File.Exists).Select(x => new FileInfo(x))
                .Union(selectedDirectoryFiles).GroupBy(x => x.FullName).Select(x => x.First()).OrderBy(x => x.FullName)
                .Select(x => x.Name).ToList();

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
    ///     unless you want Virtual Files to be written to the file system - this could present many risks.
    ///     This method DOES NOT handle Virtual Directories - only virtual files, this leads to some
    ///     inconsistent behavior based on source (2025/1/12 - I am not sure how to work with Virtual
    ///     Directories)...
    /// </summary>
    /// <param name="dropInfo"></param>
    /// <param name="temporaryDirectory"></param>
    /// <param name="topLevelFolderFiles">If a FileDrop DataFormat is found take the top level files from any directory.</param>
    /// <param name="fileExtensionFilter"></param>
    /// <returns></returns>
    public static List<string> DroppedFiles(IDropInfo dropInfo, DirectoryInfo temporaryDirectory,
        bool topLevelFolderFiles = false, List<string>? fileExtensionFilter = null)
    {
        fileExtensionFilter ??= [];

        if (dropInfo.Data is not IDataObject systemDataObject) return [];

        if (systemDataObject.GetDataPresent(DataFormats.FileDrop))
        {
            if (systemDataObject.GetData(DataFormats.FileDrop) is not string[] dropFileList ||
                !dropFileList.Any()) return [];

            //2025-01-12: The top level files only is the functionality that is currently needed - I'm also leary of how to handle a drop
            //of a massive directory...
            var selectedDirectoryFiles = topLevelFolderFiles
                ? dropFileList.Where(Directory.Exists).Select(x => new DirectoryInfo(x))
                    .SelectMany(x => x.GetFiles("*.*", SearchOption.TopDirectoryOnly)).OrderBy(x => x.FullName).ToList()
                : [];

            var fileDropFileNamesList = dropFileList.Where(File.Exists).Select(x => new FileInfo(x))
                .Union(selectedDirectoryFiles).GroupBy(x => x.FullName).Select(x => x.First()).OrderBy(x => x.FullName)
                .Select(x => x.FullName).ToList();

            return fileExtensionFilter.Any()
                ? fileDropFileNamesList.Where(x =>
                    fileExtensionFilter.Contains(Path.GetExtension(x), StringComparer.OrdinalIgnoreCase)).ToList()
                : fileDropFileNamesList.ToList();
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