using System.Diagnostics.CodeAnalysis;

namespace CourtTimeline;

/// <summary>A host-owned snapshot. Dates are local calendar dates, without time zone conversion.</summary>
public sealed record CourtTimelineData
{
    [SetsRequiredMembers]
    public CourtTimelineData() { }

    public required DateOnly Start { get; init; }
    /// <summary>Exclusive axis boundary. Stage end dates, in contrast, are inclusive.</summary>
    public required DateOnly End { get; init; }
    /// <summary>Override for reproducible previews. Null uses the local current date.</summary>
    public DateOnly? Today { get; init; }
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
    public required DateOnly Start { get; init; }
    /// <summary>Last occupied calendar day, inclusive.</summary>
    public required DateOnly End { get; init; }
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
