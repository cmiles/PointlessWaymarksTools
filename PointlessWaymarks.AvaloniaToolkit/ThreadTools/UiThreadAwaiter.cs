using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace PointlessWaymarks.AvaloniaToolkit.ThreadTools;

/// <summary>
/// Original code from https://github.com/kekyo/Epoxy - Apache-2.0 license - there may
/// be other origins to this style of code but the first place I read about a UiThreadSwitcher
/// ResumeForegroundAsync/ResumeBackgroundAsync like pattern for C# was from Raymond Chen in
/// https://devblogs.microsoft.com/oldnewthing/20190329-00/?p=102373
/// </summary>
public sealed class UiThreadAwaiter : ICriticalNotifyCompletion
{
    private bool _isBound;

    internal UiThreadAwaiter()
    {
    }

    public bool IsCompleted { get; private set; }

    public void OnCompleted(Action continuation)
    {
        InternalUiThread.ContinueOnUiThread(isBound =>
        {
            _isBound = isBound;
            IsCompleted = true;
            continuation();
        });
    }

    public void UnsafeOnCompleted(Action continuation)
    {
        InternalUiThread.ContinueOnUiThread(isBound =>
        {
            _isBound = isBound;
            IsCompleted = true;
            continuation();
        });
    }

    public void GetResult()
    {
        Debug.Assert(IsCompleted);
        if (!_isBound)
            throw new InvalidOperationException(
                "Epoxy: Could not bind to UI thread. UI thread is not found.");
    }
}