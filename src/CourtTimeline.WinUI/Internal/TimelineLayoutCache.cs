namespace CourtTimeline;

// A viewport notification is not an instruction to rebuild. Explicit Refresh invalidates
// this cache so host-owned mutable collections keep their documented refresh behavior.
internal sealed class TimelineLayoutCache
{
    private CourtTimelineData? _data;
    private DateOnly _today;
    private double _width, _zoom;
    private bool _invalidated = true;

    internal TimelineLayout.Result? Result { get; private set; }
    internal void Invalidate() => _invalidated = true;

    internal bool Update(CourtTimelineData? data, DateOnly today, double viewportWidth, double zoom)
    {
        if (data is not null && (!double.IsFinite(viewportWidth) || viewportWidth <= 0)) return false;
        // Ignore sub-DIP layout jitter and changes below the minimum content width.
        double width = double.IsFinite(viewportWidth) && viewportWidth > 0 ? Math.Round(Math.Max(480, viewportWidth)) : 0;
        if (!_invalidated && ReferenceEquals(data, _data) && today == _today && width == _width && zoom == _zoom)
            return false;

        var result = data is null ? null : TimelineLayout.Calculate(data, today, width, zoom);
        (_data, _today, _width, _zoom) = (data, today, width, zoom);
        Result = result;
        _invalidated = false;
        return true;
    }
}
