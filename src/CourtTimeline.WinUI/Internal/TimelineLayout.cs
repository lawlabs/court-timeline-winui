namespace CourtTimeline;

// Pure calendar/layout logic, shared with the cross-platform tests.
internal static class TimelineLayout
{
    internal const double LabelWidth = 166;
    internal const double EventWidth = 138;
    internal const double EventGap = 8;

    internal sealed record EventPlacement(CourtEvent Event, double Point, double Left, int Lane);
    internal sealed record Row(CourtStage Stage, double Top, double Height, double Left,
        double Width, double FactWidth, DateOnly VisualEnd, IReadOnlyList<EventPlacement> Events);
    internal sealed record Result(double Width, double Height, double DayWidth, IReadOnlyList<Row> Rows)
    {
        internal double X(DateOnly date, DateOnly start) => LabelWidth + (date.DayNumber - start.DayNumber) * DayWidth;
        internal bool Contains(CourtTimelineSelection selection) => selection.Kind switch
        {
            CourtTimelineItemKind.Stage => Rows.Any(row => row.Stage.Id == selection.Id && row.Width > 0),
            CourtTimelineItemKind.Event => Rows.Any(row => row.Events.Any(e => e.Event.Id == selection.Id)),
            _ => false
        };
    }

    internal static void Validate(CourtTimelineData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.End <= data.Start) throw new ArgumentException("End must be after Start (exclusive axis boundary).", nameof(data));
        ArgumentNullException.ThrowIfNull(data.Stages);
        ArgumentNullException.ThrowIfNull(data.Events);
        var stages = new HashSet<string>(StringComparer.Ordinal);
        foreach (var stage in data.Stages)
        {
            if (stage is null || string.IsNullOrWhiteSpace(stage.Id) || !stages.Add(stage.Id))
                throw new ArgumentException("Stages must have nonempty, unique IDs.", nameof(data));
            if (stage.End < stage.Start) throw new ArgumentException($"Stage '{stage.Id}' ends before it starts.", nameof(data));
            if (!Enum.IsDefined(stage.State) || !Enum.IsDefined(stage.Tone))
                throw new ArgumentException($"Stage '{stage.Id}' has an unknown state or tone.", nameof(data));
        }
        var events = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in data.Events)
        {
            if (item is null || string.IsNullOrWhiteSpace(item.Id) || !events.Add(item.Id))
                throw new ArgumentException("Events must have nonempty, unique IDs.", nameof(data));
            if (!stages.Contains(item.StageId)) throw new ArgumentException($"Event '{item.Id}' references an unknown stage.", nameof(data));
        }
    }

    internal static Result Calculate(CourtTimelineData data, DateOnly today, bool showPlan, double viewportWidth, double zoom)
    {
        Validate(data);
        if (!double.IsFinite(viewportWidth) || viewportWidth <= 0) throw new ArgumentOutOfRangeException(nameof(viewportWidth));
        if (!double.IsFinite(zoom) || zoom is < 1 or > 4) throw new ArgumentOutOfRangeException(nameof(zoom));
        // At normal desktop widths Fit occupies exactly the viewport. Very narrow hosts scroll.
        double width = Math.Max(480, viewportWidth) * zoom;
        double dayWidth = (width - LabelWidth - 24) / (data.End.DayNumber - data.Start.DayNumber);
        double X(int day) => LabelWidth + (day - data.Start.DayNumber) * dayWidth;
        int Clip(int day) => Math.Clamp(day, data.Start.DayNumber, data.End.DayNumber);
        var rows = new List<Row>();
        double top = 76;
        var eventsByStage = data.Events.ToLookup(item => item.StageId, StringComparer.Ordinal);
        foreach (var stage in data.Stages)
        {
            if (!showPlan && stage.State == CourtStageState.Potential) continue;
            int start = Clip(stage.Start.DayNumber);
            // DayNumber + 1 avoids overflowing DateOnly.MaxValue.
            int end = Clip(stage.End.DayNumber + 1);
            int factEnd = stage.State switch
            {
                CourtStageState.Completed => end,
                CourtStageState.Active => Math.Clamp(today.DayNumber + 1, start, Math.Max(start, end)),
                _ => start
            };
            if (!showPlan) end = factEnd;
            double left = X(start);
            var laneEnds = new List<double>();
            var placements = new List<EventPlacement>();
            foreach (var item in eventsByStage[stage.Id]
                .Where(e => (showPlan || !e.Planned) && e.Date >= data.Start && e.Date < data.End)
                .OrderBy(e => e.Date).ThenBy(e => e.Id, StringComparer.Ordinal))
            {
                double point = X(item.Date.DayNumber) + dayWidth / 2;
                double labelLeft = Math.Clamp(point - 12, LabelWidth, width - EventWidth - 8);
                int lane = laneEnds.FindIndex(laneEnd => laneEnd + EventGap <= labelLeft);
                if (lane < 0) { lane = laneEnds.Count; laneEnds.Add(0); }
                laneEnds[lane] = labelLeft + EventWidth;
                placements.Add(new(item, point, labelLeft, lane));
            }
            bool hasBand = end > start;
            if (!hasBand && placements.Count == 0) continue;
            double height = Math.Max(164, 88 + laneEnds.Count * 44);
            var visualEnd = DateOnly.FromDayNumber(Math.Max(start, end - 1));
            rows.Add(new(stage, top, height, left, Math.Max(0, X(end) - left),
                Math.Max(0, X(Math.Min(factEnd, end)) - left), visualEnd, placements));
            top += height;
        }
        return new(width, Math.Max(200, top), dayWidth, rows);
    }
}
