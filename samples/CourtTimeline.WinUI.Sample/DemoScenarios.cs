namespace CourtTimeline.Sample;

internal sealed record DemoScenario(string Title, string Description, Func<CourtTimelineData?> Create,
    CourtTimelineSelection? InitialSelection = null);

internal static class DemoScenarios
{
    internal static IReadOnlyList<DemoScenario> All { get; } =
    [
        new("Демонстрационное дело",
            "Закрытая первая инстанция, апелляция с окном до факта и сроком 16 октября, кассация ещё не началась. Сегодня — 19 сентября 2026.",
            DemoData.Load, new(CourtTimelineItemKind.Event, "hearing-appeal")),
        new("Без обещанного срока",
            "Срок закрытия не задан: полоса доходит до сегодня. Дальше она тянется только до уже назначенного заседания 6 октября.",
            OpenEdge, new(CourtTimelineItemKind.Event, "hearing")),
        new("Срок ещё впереди",
            "Черновой срок 16 октября задан в данных. До сегодня полоса сплошная, после — штриховка. Контрол этот срок не вычисляет.",
            FutureDeadline, new(CourtTimelineItemKind.Event, "deadline")),
        new("Срок уже прошёл",
            "Срок 20 августа раньше сегодня, а дата закрытия не записана. В прошлое штриховки нет: полоса заканчивается сегодня.",
            ExpiredDeadline, new(CourtTimelineItemKind.Stage, "appeal")),
        new("Стадия закрыта",
            "Стадия закрыта 2 сентября. Сегодня позже, но полоса к нему не тянется. Слева — окно, когда она уже была возможна, но ещё не началась.",
            ClosedStage, new(CourtTimelineItemKind.Stage, "appeal")),
        new("Ещё не началась",
            "Фактической карточки нет. Вся полоса — предложение начать стадию, с черновым концом 17 декабря.",
            NotStarted, new(CourtTimelineItemKind.Event, "possible-hearing")),
        new("Много событий в один день", "Четырнадцать дополнительных событий на 14 августа. Проверьте подписи и вертикальную прокрутку.", Dense),
        new("Новый год и 29 февраля", "Декабрь 2027 — март 2028. Закрытая стадия переходит через новый год, заседание стоит на 29 февраля.", YearBoundary),
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
                Status = "Завершена", Tone = CourtStageTone.Blue, Start = new(2027, 12, 20), Closed = new(2028, 1, 5) },
            new() { Id = "appeal", Title = "Апелляция", Court = "Демонстрационный суд", State = CourtStageState.Active,
                Status = "В производстве", Start = new(2028, 1, 6), Deadline = new(2028, 3, 5) }
        ],
        Events =
        [
            new() { Id = "december", StageId = "first", Date = new(2027, 12, 31), Title = "Документы представлены", Kind = "Документ" },
            new() { Id = "january", StageId = "first", Date = new(2028, 1, 5), Title = "Решение суда", Kind = "Судебный акт" },
            new() { Id = "today", StageId = "appeal", Date = new(2028, 1, 12), Title = "Жалоба принята", Kind = "Определение" },
            new() { Id = "leap", StageId = "appeal", Date = new(2028, 2, 29), Time = "11:00", Title = "Заседание 29 февраля", Kind = "Заседание", Planned = true }
        ]
    };

    private static readonly DateOnly SeptemberToday = new(2026, 9, 19);

    private static CourtTimelineData OpenEdge() => Picture(
        "Апелляция без срока", "Срок закрытия не обещан", "Апелляция · продолжается",
        new(2026, 8, 1), new(2026, 11, 1),
        new()
        {
            Id = "appeal", Title = "Апелляция", Court = "9-й арбитражный АС", State = CourtStageState.Active,
            Status = "В производстве", Start = new(2026, 8, 4),
            Note = "Дата закрытия не задана. Правый хвост есть только потому, что на 6 октября уже назначено заседание. Это не расчёт процессуального срока."
        },
        [
            new() { Id = "filed", StageId = "appeal", Date = new(2026, 8, 4), Title = "Жалоба подана", Kind = "Подача документа" },
            new() { Id = "today", StageId = "appeal", Date = SeptemberToday, Title = "Определение сегодня", Kind = "Определение" },
            new() { Id = "hearing", StageId = "appeal", Date = new(2026, 10, 6), Time = "11:00", Title = "Судебное заседание", Kind = "Заседание", Planned = true,
                Note = "Назначенная дата. Полоса дотягивается до этого дня, но стадия от этого не закрывается." }
        ]);

    private static CourtTimelineData FutureDeadline() => Picture(
        "Апелляция со сроком", "Черновой срок задан вручную", "Апелляция · в производстве",
        new(2026, 7, 15), new(2026, 11, 1),
        new()
        {
            Id = "appeal", Title = "Апелляция", Court = "9-й арбитражный АС", State = CourtStageState.Active,
            Status = "В производстве", Start = new(2026, 8, 4), Deadline = new(2026, 10, 16), DeadlineVia = "план завершения",
            Note = "16 октября — черновая дата в данных демонстрации, не срок, посчитанный контролом."
        },
        [
            new() { Id = "filed", StageId = "appeal", Date = new(2026, 8, 4), Title = "Жалоба подана", Kind = "Подача документа" },
            new() { Id = "hearing", StageId = "appeal", Date = new(2026, 9, 29), Time = "11:00", Title = "Судебное заседание", Kind = "Заседание", Planned = true },
            new() { Id = "deadline", StageId = "appeal", Date = new(2026, 10, 16), Title = "План завершения", Kind = "Плановая дата", Planned = true,
                Note = "Событие стоит в день обещанного срока. Само по себе оно срок не создаёт: срок задан у стадии." }
        ]);

    private static CourtTimelineData ExpiredDeadline() => Picture(
        "Просроченный срок", "Обещание уже в прошлом", "Апелляция · срок прошёл",
        new(2026, 5, 15), new(2026, 10, 15),
        new()
        {
            Id = "appeal", Title = "Апелляция", Court = "9-й арбитражный АС", State = CourtStageState.Active,
            Status = "Срок прошёл", Start = new(2026, 6, 1), Deadline = new(2026, 8, 20), DeadlineVia = "план завершения",
            Note = "20 августа уже раньше сегодня, а дата закрытия не записана. Штриховать август задним числом было бы неверно."
        },
        [
            new() { Id = "filed", StageId = "appeal", Date = new(2026, 6, 1), Title = "Жалоба подана", Kind = "Подача документа" },
            new() { Id = "missed", StageId = "appeal", Date = new(2026, 8, 20), Title = "День обещанного срока", Kind = "Плановая дата",
                Note = "День срока уже прошёл. Полоса всё равно заканчивается сегодня, без штриховки назад." }
        ]);

    private static CourtTimelineData ClosedStage() => Picture(
        "Закрытая апелляция", "Факт не тянется к сегодня", "Апелляция · завершена",
        new(2026, 6, 15), new(2026, 10, 1),
        new()
        {
            Id = "appeal", Title = "Апелляция", Court = "9-й арбитражный АС", State = CourtStageState.Completed,
            Status = "Завершена", Tone = CourtStageTone.Blue, PossibleFrom = new(2026, 7, 1), Start = new(2026, 7, 22), Closed = new(2026, 9, 2),
            Note = "С 1 июля стадия уже была возможна, с 22 июля она шла фактически и закрылась 2 сентября. Сегодня позже этой даты."
        },
        [
            new() { Id = "filed", StageId = "appeal", Date = new(2026, 7, 22), Title = "Жалоба подана", Kind = "Подача документа" },
            new() { Id = "decision", StageId = "appeal", Date = new(2026, 9, 2), Title = "Постановление", Kind = "Судебный акт" }
        ]);

    private static CourtTimelineData NotStarted() => Picture(
        "Возможная кассация", "Стадия ещё не началась", "Кассация · возможна",
        new(2026, 9, 1), new(2027, 1, 1),
        new()
        {
            Id = "cassation", Title = "Кассация", Court = "АС Московского округа", State = CourtStageState.Potential,
            Status = "Возможная стадия", Tone = CourtStageTone.Purple, Start = new(2026, 10, 17), Deadline = new(2026, 12, 17),
            Note = "Фактического начала нет. Полоса целиком показывает предложение, а не состоявшееся производство."
        },
        [
            new() { Id = "possible-hearing", StageId = "cassation", Date = new(2026, 11, 12), Time = "10:00", Title = "Возможное заседание", Kind = "Заседание", Planned = true,
                Note = "Условная дата для ещё не начатой стадии. Это не назначение суда." }
        ]);

    private static CourtTimelineData Picture(string title, string subject, string status, DateOnly start, DateOnly end, CourtStage stage, CourtEvent[] events) => new()
    {
        Number = "ДЕМО-СТАДИИ", Title = title, Subject = subject, Status = status,
        Start = start, End = end, Today = SeptemberToday, Stages = [stage], Events = events
    };

    private static CourtTimelineData EmptyCase() => DemoData.Load() with
    {
        Title = "Новое дело", Status = "Событий пока нет", Stages = [], Events = []
    };
}
