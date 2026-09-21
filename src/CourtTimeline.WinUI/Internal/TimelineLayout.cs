namespace CourtTimeline;

// Pure calendar/layout logic, shared with the cross-platform tests.
internal static class TimelineLayout
{
    internal const double LabelWidth = 166;
    internal const double EventWidth = 138;
    internal const double EventGap = 8;

    internal sealed record EventPlacement(CourtEvent Event, double Point, double Left, int Lane);
    internal enum StageEdge { None, Open, Deadline, Expired }

    internal sealed record Row(CourtStage Stage, double Top, double Height, double Left,
        double Width, double LeadWidth, double FactWidth, StageEdge Edge, DateOnly VisualStart, DateOnly VisualEnd,
        IReadOnlyList<EventPlacement> Events);
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
            if (stage.PossibleFrom is { } possible && possible >= stage.Start)
                throw new ArgumentException($"Stage '{stage.Id}' becomes possible on or after it starts.", nameof(data));
            if (stage.Closed is { } closed && closed < stage.Start)
                throw new ArgumentException($"Stage '{stage.Id}' closes before it starts.", nameof(data));
            if (stage.Deadline is { } deadline && deadline < stage.Start)
                throw new ArgumentException($"Stage '{stage.Id}' deadline is before it starts.", nameof(data));
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

    internal static Result Calculate(CourtTimelineData data, DateOnly today, double viewportWidth, double zoom)
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
        int tomorrow = today.DayNumber + 1;
        foreach (var stage in data.Stages)
        {
            int factStart = stage.Start.DayNumber;
            int bandStart = stage.PossibleFrom is { } possible ? possible.DayNumber : factStart;
            bool closed = stage.State != CourtStageState.Potential && (stage.State == CourtStageState.Completed || stage.Closed is not null);
            int InclusiveEnd(DateOnly? day, int fallback) => (day?.DayNumber ?? fallback) + 1;
            int bandEnd;
            var edge = StageEdge.None;
            if (stage.State == CourtStageState.Potential)
                bandEnd = InclusiveEnd(stage.Deadline, factStart);
            else if (closed)
                bandEnd = InclusiveEnd(stage.Closed ?? stage.Deadline, factStart);
            else if (stage.Deadline is { } promise && promise.DayNumber > today.DayNumber)
            {
                bandEnd = promise.DayNumber + 1;
                edge = StageEdge.Deadline;
            }
            else if (stage.Deadline is not null)
            {
                bandEnd = tomorrow;
                edge = StageEdge.Expired;
            }
            else
            {
                bandEnd = tomorrow;
                edge = StageEdge.Open;
            }
            foreach (var item in eventsByStage[stage.Id])
            {
                if (item.Date < data.Start || item.Date >= data.End) continue;
                bandEnd = Math.Max(bandEnd, item.Date.DayNumber + 1);
            }
            // Fact includes the whole of today. Only a future deadline, or a later linked event, continues as hatch.
            int factEnd = stage.State == CourtStageState.Potential
                ? factStart
                : closed ? bandEnd : Math.Min(bandEnd, Math.Max(factStart, tomorrow));
            if (bandEnd < bandStart) bandStart = bandEnd;
            if (bandEnd > data.End.DayNumber || bandEnd <= bandStart) edge = StageEdge.None;
            int drawStart = Clip(bandStart);
            int drawEnd = Clip(bandEnd);
            double left = X(drawStart);
            var laneEnds = new List<double>();
            var placements = new List<EventPlacement>();
            foreach (var item in eventsByStage[stage.Id]
                .Where(e => e.Date >= data.Start && e.Date < data.End)
                .OrderBy(e => e.Date).ThenBy(e => e.Id, StringComparer.Ordinal))
            {
                double point = X(item.Date.DayNumber) + dayWidth / 2;
                double labelLeft = Math.Clamp(point - 12, LabelWidth, width - EventWidth - 8);
                int lane = laneEnds.FindIndex(laneEnd => laneEnd + EventGap <= labelLeft);
                if (lane < 0) { lane = laneEnds.Count; laneEnds.Add(0); }
                laneEnds[lane] = labelLeft + EventWidth;
                placements.Add(new(item, point, labelLeft, lane));
            }
            bool hasBand = drawEnd > drawStart;
            if (!hasBand && placements.Count == 0) continue;
            int leadDay = Math.Clamp(factStart, drawStart, drawEnd);
            int factDay = Math.Clamp(factEnd, drawStart, drawEnd);
            double lead = X(leadDay) - left;
            double fact = Math.Max(0, X(factDay) - X(Math.Max(drawStart, factStart)));
            double height = Math.Max(164, 88 + laneEnds.Count * 44);
            var visualStart = DateOnly.FromDayNumber(hasBand ? drawStart : stage.Start.DayNumber);
            var visualEnd = DateOnly.FromDayNumber(hasBand ? drawEnd - 1 : stage.Start.DayNumber);
            rows.Add(new(stage, top, height, left, hasBand ? X(drawEnd) - left : 0, lead, fact, hasBand ? edge : StageEdge.None,
                visualStart, visualEnd, placements));
            top += height;
        }
        return new(width, Math.Max(200, top), dayWidth, rows);
    }
}
