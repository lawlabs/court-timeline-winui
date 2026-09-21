using System.Diagnostics.CodeAnalysis;

namespace CourtTimeline;

/// <summary>A host-owned snapshot. Dates are local calendar dates, without time zone conversion.</summary>
public sealed record CourtTimelineData
{
    [SetsRequiredMembers]
    public CourtTimelineData() { }

    public required DateOnly Start { get; init; }
    /// <summary>Exclusive axis boundary. <see cref="CourtStage.Closed"/> and <see cref="CourtStage.Deadline"/> include the complete last day.</summary>
    public required DateOnly End { get; init; }
    /// <summary>Override for reproducible previews. Null uses the local current date.</summary>
    public DateOnly? Today { get; init; }
    /// <summary>Clock shown on the today marker. It does not move the marker: the marker is the start of the calendar day, and that whole day counts as fact.</summary>
    public TimeOnly? Now { get; init; }
    public string Number { get; init; } = "";
    public string Title { get; init; } = "";
    public string Subject { get; init; } = "";
    public string Amount { get; init; } = "";
    public string Status { get; init; } = "";
    public IReadOnlyList<CourtStage> Stages { get; init; } = [];
    public IReadOnlyList<CourtEvent> Events { get; init; } = [];
}

public enum CourtStageState { Completed, Active, Potential }
public enum CourtStageTone { Blue, Green, Purple }

public sealed record CourtStage
{
    public required string Id { get; init; }
    public string Title { get; init; } = "";
    public string Court { get; init; } = "";
    public string Status { get; init; } = "";
    public CourtStageState State { get; init; }
    public CourtStageTone Tone { get; init; } = CourtStageTone.Green;
    /// <summary>First day the stage became possible, when that day is earlier than <see cref="Start"/>. Draws the waiting hatch to the left of the facts.</summary>
    public DateOnly? PossibleFrom { get; init; }
    /// <summary>First factual day. For a potential stage that has not started, the first planned day.</summary>
    public required DateOnly Start { get; init; }
    /// <summary>Last factual day, inclusive. Set when the stage is closed. The whole closed interval is fact, including days after "today".</summary>
    public DateOnly? Closed { get; init; }
    /// <summary>Promised last day, inclusive. On an active stage this is the only reason to hatch a future tail. Null leaves an open edge at today. On a potential stage this is the planned end.</summary>
    public DateOnly? Deadline { get; init; }
    /// <summary>Short reason for <see cref="Deadline"/>, shown in the future hatch. The host supplies it; the control calculates no legal term.</summary>
    public string DeadlineVia { get; init; } = "";
    public string Note { get; init; } = "";
}

public sealed record CourtEvent
{
    public required string Id { get; init; }
    public required string StageId { get; init; }
    public required DateOnly Date { get; init; }
    /// <summary>Optional host-formatted time; the renderer performs no time zone conversion.</summary>
    public string Time { get; init; } = "";
    public string Title { get; init; } = "";
    public string Kind { get; init; } = "";
    public string Location { get; init; } = "";
    public string Note { get; init; } = "";
    public bool Planned { get; init; }
}

public enum CourtTimelineItemKind { Stage, Event }

/// <summary>Stage and event IDs occupy separate namespaces.</summary>
public sealed record CourtTimelineSelection(CourtTimelineItemKind Kind, string Id);

public sealed class CourtTimelineSelectionChangedEventArgs(
    CourtTimelineSelection? selection, CourtStage? stage, CourtEvent? item) : EventArgs
{
    public CourtTimelineSelection? Selection { get; } = selection;
    public CourtStage? Stage { get; } = stage;
    public CourtEvent? Event { get; } = item;
}
