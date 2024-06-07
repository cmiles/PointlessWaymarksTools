using Avalonia.Threading;

namespace PointlessWaymarks.AvaloniaToolkit.ThreadTools;

internal static partial class InternalUiThread
{
    private static readonly ThreadLocal<bool?> Ids = new();

    private static bool InternalIsBound()
    {
        var dispatcher = Dispatcher.UIThread;
        if (dispatcher.CheckAccess()) return true;

        return false;
    }

    public static ValueTask<bool> IsBoundAsync()
    {
        switch (Ids.Value)
        {
            case true:
                return new ValueTask<bool>(true);
            case false:
                return new ValueTask<bool>(false);
            default:

                var f = InternalIsBound();
                Ids.Value = f;
                return new ValueTask<bool>(f);
        }
    }

    public static bool UnsafeIsBound()
    {
        switch (Ids.Value)
        {
            case true:
                return true;
            case false:
                break;
            default:
                var f = InternalIsBound();
                Ids.Value = f;
                if (f) return true;

                break;
        }

        return false;
    }

    public static void ContinueOnWorkerThread(Action continuation)
    {
        if (UnsafeIsBound())
            ThreadPool.QueueUserWorkItem(_ => continuation(), null);
        else
            continuation();
    }

    public static void ContinueOnUiThread(Action<bool> continuation)
    {
        if (Dispatcher.UIThread is { } dispatcher)
        {
            if (dispatcher.CheckAccess())
                continuation(true);
            else
                dispatcher.Post(() => continuation(true));
        }
        else
        {
            continuation(false);
        }
    }
}