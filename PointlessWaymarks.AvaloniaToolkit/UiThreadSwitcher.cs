using System.Diagnostics;
using PointlessWaymarks.AvaloniaToolkit.ThreadTools;

namespace PointlessWaymarks.AvaloniaToolkit;

/// <summary>
///     UI thread commonly manipulator.
///     Original code from https://github.com/kekyo/Epoxy - Apache-2.0 license - there may
///     be other origins to this style of code but the first place I read about a UiThreadSwitcher
///     ResumeForegroundAsync/ResumeBackgroundAsync like pattern for C# was from Raymond Chen in
///     https://devblogs.microsoft.com/oldnewthing/20190329-00/?p=102373
/// </summary>
[DebuggerStepThrough]
public static class UiThreadSwitcher
{
    /// <summary>
    ///     Binds current task to the UI thread context manually.
    /// </summary>
    /// <returns>Awaitable UI thread object.</returns>
    /// <example>
    ///     <code>
    /// // (On the arbitrary thread context here)
    /// 
    /// // Switch to UI thread context uses async-await.
    /// await UIThread.ResumeForegroundAsync();
    /// 
    /// // (On the UI thread context here)
    /// </code>
    /// </example>
    public static UiThreadAwaitable ResumeForegroundAsync()
    {
        return new UiThreadAwaitable();
    }

    /// <summary>
    ///     Unbinds current UI task to the worker thread context manually.
    /// </summary>
    /// <returns>Awaitable worker thread object.</returns>
    /// <example>
    ///     <code>
    /// // (On the UI thread context here)
    /// 
    /// // Switch to worker thread context uses async-await.
    /// await UIThread.ResumeBackgroundAsync();
    /// 
    /// // (On the worker thread context here)
    /// </code>
    /// </example>
    public static UiThreadUnbindAwaitable ResumeBackgroundAsync()
    {
        return new UiThreadUnbindAwaitable();
    }
}