namespace CourtTimeline.Sample;

internal sealed record DemoScenario(string Title, string Description, Func<CourtTimelineData?> Create,
    CourtTimelineSelection? InitialSelection = null);

internal static class DemoScenarios
{
    internal static IReadOnlyList<DemoScenario> All { get; } =
    [
        new("Демонстрационное дело", "Три стадии, восемь событий. Дата зафиксирована: 19 сентября 2026.",
            DemoData.Load, new(CourtTimelineItemKind.Event, "hearing-appeal")),
        new("Много событий в один день", "Четырнадцать дополнительных событий на 14 августа. Проверьте подписи и вертикальную прокрутку.", Dense),
        new("Новый год и 29 февраля", "Декабрь 2027 — март 2028. Проверьте смену года, високосный день и даты в деталях.", YearBoundary),
        new("Только план", "Нет активной стадии. Отключение плана должно показать пустое состояние и очистить выбор.",
            PlannedOnly, new(CourtTimelineItemKind.Event, "planned-hearing")),
        new("Пустое дело", "Карточка дела без стадий и событий. Счётчики должны показывать нули.", EmptyCase),
        new("Нет выбранного дела", "Данные отсутствуют. Шапка дела скрывается, старые детали очищаются.", () => null)
    ];

    private static CourtTimelineData Dense()
    {
        var demo = DemoData.Load();
        var extra = Enumerable.Range(0, 14).Select(index => new CourtEvent
        {
            Id = $"dense-{index:00}", StageId = "appeal", Date = new(2026, 8, 14),
            Time = $"{9 + index / 4:00}:{index % 4 * 15:00}",
            Title = $"Документ {index + 1}: дополнительные доказательства и пояснения стороны",
            Kind = "Документ", Location = "9-й арбитражный апелляционный суд",
            Note = "Длинный текст для проверки переноса строк в деталях. События одного дня должны оставаться доступными при любом масштабе."
        });
        return demo with { Title = "Проверка плотной раскладки", Events = demo.Events.Concat(extra).ToArray() };
    }

    private static CourtTimelineData YearBoundary() => new()
    {
        Number = "ДЕМО-2027/2028", Title = "Переход между годами", Subject = "Проверка календарных границ",
        Start = new(2027, 12, 15), End = new(2028, 3, 15), Today = new(2028, 1, 12),
        Stages =
        [
            new() { Id = "first", Title = "Первая инстанция", Court = "Демонстрационный суд", State = CourtStageState.Completed,
                Status = "Завершена", Tone = CourtStageTone.Blue, Start = new(2027, 12, 20), End = new(2028, 1, 5) },
            new() { Id = "appeal", Title = "Апелляция", Court = "Демонстрационный суд", State = CourtStageState.Active,
                Status = "В производстве", Start = new(2028, 1, 6), End = new(2028, 3, 5) }
        ],
        Events =
        [
            new() { Id = "december", StageId = "first", Date = new(2027, 12, 31), Title = "Документы представлены", Kind = "Документ" },
            new() { Id = "january", StageId = "first", Date = new(2028, 1, 5), Title = "Решение суда", Kind = "Судебный акт" },
            new() { Id = "today", StageId = "appeal", Date = new(2028, 1, 12), Title = "Жалоба принята", Kind = "Определение" },
            new() { Id = "leap", StageId = "appeal", Date = new(2028, 2, 29), Time = "11:00", Title = "Заседание 29 февраля", Kind = "Заседание", Planned = true }
        ]
    };

    private static CourtTimelineData PlannedOnly()
    {
        var demo = DemoData.Load();
        var stage = demo.Stages.Single(item => item.State == CourtStageState.Potential);
        return demo with
        {
            Title = "Возможное продолжение дела", Status = "Только план", Stages = [stage],
            Events = [new() { Id = "planned-hearing", StageId = stage.Id, Date = stage.Start,
                Title = "Возможное заседание", Kind = "План", Planned = true, Note = "Условная дата для проверки интерфейса." }]
        };
    }

    private static CourtTimelineData EmptyCase() => DemoData.Load() with
    {
        Title = "Новое дело", Status = "Событий пока нет", Stages = [], Events = []
    };
}
