using System.Diagnostics;

namespace PointlessWaymarks.AvaloniaToolkit.ThreadTools;

/// <summary>
/// Original code from https://github.com/kekyo/Epoxy - Apache-2.0 license - there may
/// be other origins to this style of code but the first place I read about a UiThreadSwitcher
/// ResumeForegroundAsync/ResumeBackgroundAsync like pattern for C# was from Raymond Chen in
/// https://devblogs.microsoft.com/oldnewthing/20190329-00/?p=102373
/// </summary>
[DebuggerStepThrough]
public struct UiThreadAwaitable
{
    public UiThreadAwaiter GetAwaiter() => new();
}