using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using DocumentFormat.OpenXml.EMMA;
using GongSolutions.Wpf.DragDrop;
using Ookii.Dialogs.Wpf;
using PointlessWaymarks.CommonTools;
using PointlessWaymarks.LlamaAspects;
using PointlessWaymarks.WpfCommon.Status;
using PointlessWaymarks.WpfCommon.Utility;
using static PointlessWaymarks.WpfCommon.ThreadSwitcher;

namespace PointlessWaymarks.WpfCommon.FileList;

[NotifyPropertyChanged]
[GenerateStatusCommands]
public partial class FileListContext : IDropTarget
{
    public FileListContext(StatusControlContext statusContext, IFileListSettings? settings,
        List<ContextMenuItemData> contextMenuItems)
    {
        StatusContext = statusContext;
        Settings = settings;

        BuildCommands();

        var localContextItems = new List<ContextMenuItemData>
        {
            new() { ItemCommand = OpenSelectedFileDirectoryCommand, ItemName = "Open Directory" },
            new() { ItemCommand = OpenSelectedFileDirectoryCommand, ItemName = "Open File" }
        };

        ContextMenuItems = contextMenuItems.Union(localContextItems).ToList();
    }

    public List<ContextMenuItemData> ContextMenuItems { get; set; }
    public List<string> DroppedFileExtensionAllowList { get; set; } = [];
    public string FileImportFilter { get; set; } = string.Empty;
    public ObservableCollection<FileInfo>? Files { get; set; }
    public bool ReplaceMode { get; set; } = true;
    public FileInfo? SelectedFile { get; set; }
    public ObservableCollection<FileInfo>? SelectedFiles { get; set; }
    public IFileListSettings? Settings { get; set; }
    public StatusControlContext StatusContext { get; set; }

    public void DragOver(IDropInfo dropInfo)
    {
        var files = DragAndDropFilesHelper.DroppedFileNames(dropInfo, true, DroppedFileExtensionAllowList);

        if (files.Any())
        {
            dropInfo.Effects = DragDropEffects.Copy;
            return;
        }

        dropInfo.Effects = DragDropEffects.None;
    }

    public void Drop(IDropInfo dropInfo)
    {
        StatusContext.RunBlockingTask(async () => await AddFilesFromGuiDrop(dropInfo));
    }

    public async Task AddFilesFromGuiDrop(IDropInfo dropData)
    {
        await ResumeBackgroundAsync();

        var files = DragAndDropFilesHelper.DroppedFiles(dropData, FileLocationTools.TempStorageDirectory(), true, DroppedFileExtensionAllowList);

        await ResumeForegroundAsync();

        if (ReplaceMode) Files?.Clear();

        files.ForEach(x =>
        {
            if (!Files!.Any(y => y.FullName.Equals(x, StringComparison.OrdinalIgnoreCase))) Files!.Add(new FileInfo(x));
        });
    }

    [BlockingCommand]
    public async Task AddFilesToTag()
    {
        await ResumeBackgroundAsync();

        Debug.Assert(Settings != null, nameof(Settings) + " != null");
        var lastDirectory = await Settings.GetLastDirectory();

        await ResumeForegroundAsync();

        var filePicker = new VistaOpenFileDialog
            { Title = "Add Files", Multiselect = true, CheckFileExists = true, ValidateNames = true };
        if (!string.IsNullOrWhiteSpace(FileImportFilter)) filePicker.Filter = FileImportFilter;

        if (lastDirectory != null) filePicker.FileName = $"{lastDirectory.FullName}\\";

        var result = filePicker.ShowDialog();

        if (!result ?? false) return;

        if (ReplaceMode) Files?.Clear();

        await Settings.SetLastDirectory(Path.GetDirectoryName(filePicker.FileNames.FirstOrDefault()) ?? string.Empty);

        var selectedFiles = filePicker.FileNames.Select(x => new FileInfo(x)).Where(x => !Files!.Contains(x))
            .ToList();

        selectedFiles.ForEach(x =>
        {
            if (!Files!.Any(y => y.FullName.Equals(x.FullName, StringComparison.OrdinalIgnoreCase))) Files!.Add(x);
        });
    }

    public async Task AddFilesToTag(List<string> filesToAdd)
    {
        if (!filesToAdd.Any()) return;

        await ResumeForegroundAsync();

        if (ReplaceMode) Files?.Clear();

        var selectedFiles = filesToAdd.Select(x => new FileInfo(x)).Where(x => x.Exists && !Files!.Contains(x))
            .ToList();

        selectedFiles.ForEach(x =>
        {
            if (!Files!.Any(y => y.FullName.Equals(x.FullName, StringComparison.OrdinalIgnoreCase))) Files!.Add(x);
        });
    }

    [BlockingCommand]
    public async Task AddFilesToTagFromDirectory()
    {
        await ResumeBackgroundAsync();

        Debug.Assert(Settings != null, nameof(Settings) + " != null");
        var lastDirectory = await Settings.GetLastDirectory();

        await ResumeForegroundAsync();
        var folderPicker = new VistaFolderBrowserDialog
            { Description = "Directory to Add", Multiselect = true };

        if (lastDirectory != null) folderPicker.SelectedPath = $"{lastDirectory.FullName}\\";

        var result = folderPicker.ShowDialog();

        if (!result ?? false) return;

        if (!folderPicker.SelectedPaths.Any())
        {
            await StatusContext.ToastWarning("No directories selected?");
            return;
        }

        if (ReplaceMode) Files?.Clear();

        var selectedDirectory = new DirectoryInfo(folderPicker.SelectedPaths[0]);

        if (selectedDirectory.Parent != null) await Settings.SetLastDirectory(selectedDirectory.Parent.FullName);
        else await Settings.SetLastDirectory(selectedDirectory.FullName);

        foreach (var loopPaths in folderPicker.SelectedPaths)
        {
            var loopDirectory = new DirectoryInfo(loopPaths);

            if (!loopDirectory.Exists)
            {
                await StatusContext.ToastError($"{loopDirectory.FullName} doesn't exist?");
                continue;
            }

            var selectedFiles = loopDirectory.EnumerateFiles("*").ToList().Where(x => !Files!.Contains(x))
                .ToList();

            selectedFiles.ForEach(x =>
            {
                if (!Files!.Any(y => y.FullName.Equals(x.FullName, StringComparison.OrdinalIgnoreCase))) Files!.Add(x);
            });
        }
    }

    [BlockingCommand]
    public async Task AddFilesToTagFromDirectoryAndSubdirectories()
    {
        await ResumeBackgroundAsync();

        Debug.Assert(Settings != null, nameof(Settings) + " != null");
        var lastDirectory = await Settings.GetLastDirectory();

        await ResumeForegroundAsync();
        var folderPicker = new VistaFolderBrowserDialog
            { Description = "Directory And Subdirectories to Add", Multiselect = true };
        if (lastDirectory != null) folderPicker.SelectedPath = $"{lastDirectory.FullName}\\";

        var result = folderPicker.ShowDialog();

        if (!result ?? false) return;

        if (!folderPicker.SelectedPaths.Any())
        {
            await StatusContext.ToastWarning("No directories selected?");
            return;
        }

        if (ReplaceMode) Files?.Clear();

        var selectedDirectory = new DirectoryInfo(folderPicker.SelectedPaths[0]);

        if (selectedDirectory.Parent != null) await Settings.SetLastDirectory(selectedDirectory.Parent.FullName);
        else await Settings.SetLastDirectory(selectedDirectory.FullName);

        foreach (var loopPaths in folderPicker.SelectedPaths)
        {
            var loopDirectory = new DirectoryInfo(loopPaths);

            if (!loopDirectory.Exists)
            {
                await StatusContext.ToastError($"{loopDirectory.FullName} doesn't exist?");
                continue;
            }

            var selectedFiles = loopDirectory.EnumerateFiles("*", SearchOption.AllDirectories).ToList()
                .Where(x => !Files!.Contains(x))
                .ToList();

            selectedFiles.ForEach(x =>
            {
                if (!Files!.Any(y => y.FullName.Equals(x.FullName, StringComparison.OrdinalIgnoreCase))) Files!.Add(x);
            });
        }
    }

    public static async Task<FileListContext> CreateInstance(StatusControlContext statusContext,
        IFileListSettings? settings, List<ContextMenuItemData> contextMenuItems)
    {
        var newInstance = new FileListContext(statusContext, settings, contextMenuItems);

        await ResumeForegroundAsync();

        newInstance.Files = [];
        newInstance.SelectedFiles = [];

        return newInstance;
    }

    [NonBlockingCommand]
    public async Task DeleteSelectedFiles()
    {
        await ResumeForegroundAsync();

        var toRemove = SelectedFiles?.ToList() ?? [];

        if (toRemove.Count <= 0)
        {
            await StatusContext.ToastWarning("No Files Selected to Delete");
            return;
        }

        foreach (var loopFile in toRemove) Files?.Remove(loopFile);
    }

    [NonBlockingCommand]
    private async Task OpenSelectedFile()
    {
        await ResumeBackgroundAsync();

        if (SelectedFile is not { Exists: true, Directory.Exists: true })
        {
            await StatusContext.ToastWarning("No Selected File or Selected File no longer exists?");
            return;
        }

        await ResumeForegroundAsync();

        var ps = new ProcessStartInfo(SelectedFile.FullName) { UseShellExecute = true, Verb = "open" };
        Process.Start(ps);
    }

    [BlockingCommand]
    private async Task OpenSelectedFileDirectory()
    {
        await ResumeBackgroundAsync();

        if (SelectedFile is not { Exists: true, Directory.Exists: true })
        {
            await StatusContext.ToastWarning("No Selected File or Selected File no longer exists?");
            return;
        }

        await ProcessHelpers.OpenExplorerWindowForFile(SelectedFile.FullName);
    }
}