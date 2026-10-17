namespace CourtTimeline;

/// <summary>Optional diagnostics for a completed UI update, excluding subsequent XAML layout and presentation.</summary>
public sealed class CourtTimelineRenderCompletedEventArgs(TimeSpan elapsed, bool rebuilt, int canvasChildren) : EventArgs
{
    public TimeSpan Elapsed { get; } = elapsed;
    public bool Rebuilt { get; } = rebuilt;
    public int CanvasChildren { get; } = canvasChildren;
}
