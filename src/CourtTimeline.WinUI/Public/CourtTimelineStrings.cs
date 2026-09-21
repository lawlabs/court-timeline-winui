namespace CourtTimeline;

/// <summary>Replace the record to localize the control; dates use CourtTimelineView.Culture.</summary>
public sealed record CourtTimelineStrings
{
    public static CourtTimelineStrings Russian { get; } = new();
    public static CourtTimelineStrings English { get; } = new()
    {
        CaseNumber = "CASE No. {0}", Amount = "Claim amount", Progress = "Case progress",
        Counts = "Stages: {0} · events: {1}", Fit = "Fit process",
        ZoomIn = "Zoom in", ZoomOut = "Zoom out", Axis = "COURT / STAGE",
        Today = "TODAY", Legend = "━ Actual    ▨ Window or deadline    ┄ Potential stage    ○ Event",
        Empty = "No stages or events in this period", SelectItem = "Select a stage or event to see details",
        Stage = "STAGE", Planned = "PLANNED", Occurred = "OCCURRED", Potential = "potential",
        Until = "until {0}", PossibleFrom = "Possible from {0}",
        DeadlineHint = "Draft end {0}: {1}", OverdueHint = "The {0} deadline has passed and the stage is still open",
        OpenEdgeHint = "No closing date: the bar stops at today"
    };

    public string CaseNumber { get; init; } = "ДЕЛО № {0}";
    public string Amount { get; init; } = "Сумма требований";
    public string Progress { get; init; } = "Ход дела";
    public string Counts { get; init; } = "Стадий: {0} · событий: {1}";
    public string Fit { get; init; } = "Весь процесс";
    public string ZoomIn { get; init; } = "Увеличить масштаб";
    public string ZoomOut { get; init; } = "Уменьшить масштаб";
    public string Axis { get; init; } = "ИНСТАНЦИЯ / СТАДИЯ";
    public string Today { get; init; } = "СЕГОДНЯ";
    public string Legend { get; init; } = "━ Факт    ▨ Окно или срок    ┄ Возможная стадия    ○ Событие";
    public string Empty { get; init; } = "В этом периоде нет стадий и событий";
    public string SelectItem { get; init; } = "Выберите стадию или событие, чтобы увидеть детали";
    public string Stage { get; init; } = "СТАДИЯ";
    public string Planned { get; init; } = "ЗАПЛАНИРОВАНО";
    public string Occurred { get; init; } = "СОСТОЯЛОСЬ";
    public string Potential { get; init; } = "возможно";
    public string Until { get; init; } = "до {0}";
    public string PossibleFrom { get; init; } = "Возможна с {0}";
    public string DeadlineHint { get; init; } = "Черновой срок {0}: {1}";
    public string OverdueHint { get; init; } = "Срок {0} уже прошёл, а стадия не закрыта";
    public string OpenEdgeHint { get; init; } = "Срок закрытия не задан: полоса доходит до сегодня";
}
