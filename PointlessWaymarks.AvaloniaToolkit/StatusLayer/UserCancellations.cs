﻿using PointlessWaymarks.AvaloniaToolkit.Aspects;
using PointlessWaymarks.AvaloniaToolkit.Utility;

namespace PointlessWaymarks.AvaloniaToolkit.StatusLayer;

[NotifyPropertyChanged]
public partial class UserCancellations
{
    public UserCancellations()
    {
        Cancel = new RelayCommand(() =>
        {
            CancelSource?.Cancel();
            IsEnabled = false;
            Description = "Canceling...";
        });
    }

    public RelayCommand? Cancel { get; set; }
    public CancellationTokenSource? CancelSource { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
}