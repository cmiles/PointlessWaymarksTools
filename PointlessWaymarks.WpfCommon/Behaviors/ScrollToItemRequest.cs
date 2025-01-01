using PointlessWaymarks.LlamaAspects;

namespace PointlessWaymarks.WpfCommon.Behaviors;

[NotifyPropertyChanged]
public partial class ScrollToItemRequest
{
    public ScrollToItemRequest(object? scrollTo)
    {
        ScrollTo = scrollTo;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public object? ScrollTo { get; set; }
}