using CourtTimeline.Sample;

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]

namespace CourtTimeline.Tests;

[TestClass]
public sealed class TimelineLayoutTests
{
    private static DateOnly Date(int day) => new(2026, 9, day);
    private static CourtStage Stage(string id = "stage", CourtStageState state = CourtStageState.Active) => new()
    { Id = id, Start = Date(3), Deadline = Date(25), State = state };
    private static CourtEvent Event(string id, int day = 10, bool planned = false) => new()
    { Id = id, StageId = "stage", Date = Date(day), Planned = planned };
    private static CourtTimelineData Data(params CourtEvent[] events) => new()
    { Start = Date(1), End = Date(30), Today = Date(19), Stages = [Stage()], Events = events };
    private static TimelineLayout.Result Layout(CourtTimelineData data, double width = 960, double zoom = 1) =>
        TimelineLayout.Calculate(data, data.Today ?? Date(19), width, zoom);

    [TestMethod]
    public void SampleFixtureContainsThreeStagesAndEightEvents()
    {
        var data = DemoData.Load();
        var layout = Layout(data);
        Assert.HasCount(3, layout.Rows);
        Assert.AreEqual(8, layout.Rows.Sum(row => row.Events.Count));
    }

    [TestMethod]
    public void StageEndIncludesTheWholeLastDay()
    {
        var layout = Layout(Data() with { Stages = [Stage() with { Start = Date(10), Closed = Date(10), Deadline = null, State = CourtStageState.Completed }] });
        Assert.AreEqual(layout.DayWidth, layout.Rows.Single().Width, 1e-8);
        Assert.AreEqual(layout.Rows[0].Width, layout.Rows[0].FactWidth, 1e-8);
    }

    [TestMethod]
    public void ActiveStageFactIncludesTodayAndPlanStartsTomorrow()
    {
        var full = Layout(Data());
        Assert.AreEqual(17 * full.DayWidth, full.Rows[0].FactWidth, 1e-8);
        Assert.IsGreaterThan(full.Rows[0].FactWidth, full.Rows[0].Width);
        Assert.AreEqual(TimelineLayout.StageEdge.Deadline, full.Rows[0].Edge);
        Assert.AreEqual(Date(25), full.Rows[0].VisualEnd);
    }

    [TestMethod]
    public void FutureActiveStageHasNoFactOrNegativeWidth()
    {
        var row = Layout(Data() with { Today = Date(1) }).Rows.Single();
        Assert.AreEqual(0d, row.FactWidth);
        Assert.IsGreaterThan(0d, row.Width);
    }

    [TestMethod]
    public void ClosedStageStaysAtItsCloseWhenTodayIsLater()
    {
        var stage = Stage(state: CourtStageState.Completed) with { Closed = Date(25), Deadline = null };
        var row = Layout(Data() with { Today = Date(29), Stages = [stage] }).Rows.Single();
        Assert.AreEqual(row.Width, row.FactWidth);
        Assert.AreEqual(Date(25), row.VisualEnd);
        Assert.AreEqual(TimelineLayout.StageEdge.None, row.Edge);
    }

    [TestMethod]
    public void ExpiredDeadlineExtendsToTodayWithoutAPastHatch()
    {
        var row = Layout(Data() with { Today = Date(29), Stages = [Stage() with { Deadline = Date(10) }] }).Rows.Single();
        Assert.AreEqual(Date(29), row.VisualEnd);
        Assert.AreEqual(row.Width, row.FactWidth, 1e-8);
        Assert.AreEqual(0d, row.LeadWidth);
        Assert.AreEqual(TimelineLayout.StageEdge.Expired, row.Edge);
    }

    [TestMethod]
    public void OpenActiveStageEndsTodayWithoutHatch()
    {
        var layout = Layout(Data() with { Stages = [Stage() with { Deadline = null }] });
        var row = layout.Rows.Single();
        Assert.AreEqual(17 * layout.DayWidth, row.Width, 1e-8);
        Assert.AreEqual(row.Width, row.FactWidth, 1e-8);
        Assert.AreEqual(Date(3), row.VisualStart);
        Assert.AreEqual(Date(19), row.VisualEnd);
        Assert.AreEqual(TimelineLayout.StageEdge.Open, row.Edge);
    }

    [TestMethod]
    public void WaitingWindowIsHatchedAndIsNotFact()
    {
        var layout = Layout(Data() with { Stages = [Stage() with { PossibleFrom = Date(1) }] });
        var row = layout.Rows.Single();
        Assert.AreEqual(2 * layout.DayWidth, row.LeadWidth, 1e-8);
        Assert.AreEqual(17 * layout.DayWidth, row.FactWidth, 1e-8);
        Assert.AreEqual(Date(1), row.VisualStart);
        Assert.AreEqual(Date(25), row.VisualEnd);
    }

    [TestMethod]
    public void LinkedEventExtendsAnOpenStagePastToday()
    {
        var layout = Layout(Data(Event("hearing", 25)) with { Stages = [Stage() with { Deadline = null }] });
        var row = layout.Rows.Single();
        Assert.AreEqual(17 * layout.DayWidth, row.FactWidth, 1e-8);
        Assert.AreEqual(23 * layout.DayWidth, row.Width, 1e-8);
        Assert.AreEqual(Date(25), row.VisualEnd);
        Assert.AreEqual(TimelineLayout.StageEdge.Open, row.Edge);
    }

    [TestMethod]
    public void PotentialStageKeepsItsPlannedEvent()
    {
        var data = Data(Event("future", planned: true)) with { Stages = [Stage(state: CourtStageState.Potential)] };
        var layout = Layout(data);
        var row = layout.Rows.Single();
        Assert.AreEqual(0d, row.FactWidth);
        Assert.IsGreaterThan(0d, row.Width);
        Assert.IsTrue(layout.Contains(new(CourtTimelineItemKind.Event, "future")));
    }

    [TestMethod]
    public void DenseSameDayEventsHaveBoundedLanesAndSelectableOverflow()
    {
        var layout = Layout(Data(Enumerable.Range(0, 12).Select(i => Event(i.ToString())).ToArray()));
        var row = layout.Rows.Single();
        Assert.AreEqual(TimelineLayout.MaxEventLanes, row.Events.Select(e => e.Lane).Distinct().Count());
        Assert.HasCount(9, row.OverflowEvents);
        Assert.AreEqual(264d, row.Height);
        Assert.IsTrue(layout.Contains(new(CourtTimelineItemKind.Event, row.OverflowEvents[0].Id)));
        foreach (var lane in row.Events.GroupBy(e => e.Lane))
        {
            var entries = lane.OrderBy(e => e.Left).ToArray();
            for (int i = 1; i < entries.Length; i++)
                Assert.IsGreaterThanOrEqualTo(entries[i - 1].Left + TimelineLayout.EventWidth + TimelineLayout.EventGap, entries[i].Left);
        }
    }

    [TestMethod]
    [DataRow(480d, 1d, false)]
    [DataRow(1200d, 1d, false)]
    [DataRow(1200d, 4d, false)]
    [DataRow(480d, 1d, true)]
    [DataRow(1200d, 4d, true)]
    public void TwoYearOpenStageWith109EventsStaysBounded(double width, double zoom, bool sameDay)
    {
        var start = new DateOnly(2024, 8, 1);
        var end = new DateOnly(2026, 12, 1);
        var events = Enumerable.Range(0, 109).Select(i => new CourtEvent
        {
            Id = i.ToString("D3"), StageId = "stage",
            Date = start.AddDays(sameDay ? 30 : i * (end.DayNumber - start.DayNumber - 1) / 108)
        }).ToArray();
        var data = new CourtTimelineData
        {
            Start = start, End = end, Today = new(2026, 10, 17),
            Stages = [Stage() with { Start = start, Deadline = null }], Events = events
        };
        var layout = Layout(data, width, zoom);
        var row = layout.Rows.Single();
        Assert.IsLessThanOrEqualTo(264d, row.Height);
        Assert.AreEqual(109, row.Events.Count + row.OverflowEvents.Count);
        Assert.AreEqual(109, row.Events.Select(e => e.Event.Id).Concat(row.OverflowEvents.Select(e => e.Id)).Distinct().Count());
        Assert.IsTrue(events.All(e => layout.Contains(new(CourtTimelineItemKind.Event, e.Id))));
        Assert.IsTrue(row.Events.All(e => e.Lane is >= 0 and < TimelineLayout.MaxEventLanes));
        foreach (var lane in row.Events.GroupBy(e => e.Lane))
        {
            var labels = lane.OrderBy(e => e.Left).ToArray();
            Assert.IsTrue(labels.Zip(labels.Skip(1)).All(pair =>
                pair.First.Left + TimelineLayout.EventWidth + TimelineLayout.EventGap <= pair.Second.Left));
        }
    }

    [TestMethod]
    public void OverflowDoesNotPreventLaterEventsFromReusingLanes()
    {
        var events = Enumerable.Range(0, 12).Select(i => Event(i.ToString(), 3)).Append(Event("late", 25)).ToArray();
        var row = Layout(Data(events), width: 1200).Rows.Single();
        Assert.IsTrue(row.Events.Any(e => e.Event.Id == "late" && e.Lane == 0));
    }

    [TestMethod]
    public void ShortSparseCaseKeepsItsOriginalHeightAndAllLabels()
    {
        var row = Layout(Data(Event("a", 3), Event("b", 25)), width: 1200).Rows.Single();
        Assert.AreEqual(164d, row.Height);
        Assert.HasCount(2, row.Events);
        Assert.HasCount(0, row.OverflowEvents);
    }

    [TestMethod]
    public void CacheWaitsForMeasuredWidthAndRetainsPendingInvalidation()
    {
        var cache = new TimelineLayoutCache();
        var data = Data(Event("a"));
        foreach (double width in new[] { 0d, -1d, double.NaN, double.PositiveInfinity })
        {
            Assert.IsFalse(cache.Update(data, Date(19), width, 1));
            Assert.IsNull(cache.Result);
        }
        Assert.IsTrue(cache.Update(data, Date(19), 960, 1));
        var original = cache.Result;
        cache.Invalidate();
        Assert.IsFalse(cache.Update(data, Date(19), 0, 1));
        Assert.AreSame(original, cache.Result);
        Assert.IsTrue(cache.Update(data, Date(19), 960, 1));
        Assert.AreNotSame(original, cache.Result);
    }

    [TestMethod]
    public void UnchangedOpenStageReusesGeometryAcrossRepeatedNotifications()
    {
        var cache = new TimelineLayoutCache();
        var data = Data(Event("a")) with { Today = null, Stages = [Stage() with { Deadline = null }] };
        Assert.IsTrue(cache.Update(data, Date(19), 960, 1));
        var original = cache.Result;
        for (int i = 0; i < 1000; i++)
        {
            Assert.IsFalse(cache.Update(data, Date(19), i % 2 == 0 ? 960 : 960.1, 1));
            Assert.AreSame(original, cache.Result);
        }
    }

    [TestMethod]
    public void ClearingDataWhileUnmeasuredDoesNotRetainPreviousLayout()
    {
        var cache = new TimelineLayoutCache();
        Assert.IsTrue(cache.Update(Data(Event("a")), Date(19), 960, 1));
        Assert.IsNotNull(cache.Result);
        Assert.IsTrue(cache.Update(null, default, 0, 1));
        Assert.IsNull(cache.Result);
        Assert.IsFalse(cache.Update(null, default, 0, 1));
        // A newly measured empty view must be resized even though it has no layout.
        Assert.IsTrue(cache.Update(null, default, 960, 1));
    }

    [TestMethod]
    public void CacheRebuildsForDataWidthZoomCalendarDayAndExplicitRefresh()
    {
        var cache = new TimelineLayoutCache();
        var events = new List<CourtEvent> { Event("a") };
        var data = Data() with { Events = events };
        Assert.IsTrue(cache.Update(data, Date(19), 960, 1));
        Assert.IsTrue(cache.Update(data, Date(19), 1000, 1));
        Assert.IsTrue(cache.Update(data, Date(19), 1000, 2));
        Assert.IsTrue(cache.Update(data, Date(20), 1000, 2));
        data = data with { Title = "Replacement snapshot" };
        Assert.IsTrue(cache.Update(data, Date(20), 1000, 2));
        events.Add(Event("b"));
        Assert.IsFalse(cache.Update(data, Date(20), 1000, 2));
        Assert.HasCount(1, cache.Result!.Rows.Single().Events);
        cache.Invalidate();
        Assert.IsTrue(cache.Update(data, Date(20), 1000, 2));
        Assert.HasCount(2, cache.Result!.Rows.Single().Events);
    }

    [TestMethod]
    public void NonOverlappingEventsReuseLane()
    {
        var row = Layout(Data(Event("a", 3), Event("b", 25)), width: 1200).Rows.Single();
        Assert.AreEqual(row.Events[0].Lane, row.Events[1].Lane);
    }

    [TestMethod]
    public void FitUsesViewportInsteadOfPrototypeMinimum960()
    {
        Assert.AreEqual(700d, Layout(Data(), width: 700).Width);
        Assert.AreEqual(1400d, Layout(Data(), width: 700, zoom: 2).Width);
        Assert.AreEqual(480d, Layout(Data(), width: 200).Width);
    }

    [TestMethod]
    public void AxisClipsBandsAndUsesExclusiveEndForEvents()
    {
        var data = Data(Event("left", 1), Event("inside", 15), Event("right", 30)) with
        { Start = Date(5), End = Date(20), Stages = [Stage() with { Start = Date(1), Deadline = Date(30) }] };
        var layout = Layout(data);
        var row = layout.Rows.Single();
        Assert.AreEqual(TimelineLayout.LabelWidth, row.Left);
        Assert.AreEqual(layout.Width - TimelineLayout.LabelWidth - 24, row.Width, 1e-8);
        Assert.HasCount(1, row.Events);
        Assert.AreEqual("inside", row.Events[0].Event.Id);
    }

    [TestMethod]
    public void StagesOutsidePeriodAreOmitted()
    {
        var data = Data() with { Start = Date(26), End = Date(30) };
        Assert.HasCount(0, Layout(data).Rows);
    }

    [TestMethod]
    public void EmptyDataHasFiniteCanvas()
    {
        var layout = Layout(Data() with { Stages = [] });
        Assert.HasCount(0, layout.Rows);
        Assert.AreEqual(200d, layout.Height);
        Assert.IsTrue(double.IsFinite(layout.DayWidth));
    }

    [TestMethod]
    public void DateMaxDoesNotOverflowInclusiveEnd()
    {
        var data = new CourtTimelineData
        {
            Start = new(9999, 12, 1), End = DateOnly.MaxValue, Today = DateOnly.MaxValue,
            Stages = [new() { Id = "last", Start = new(9999, 12, 1), Closed = DateOnly.MaxValue }]
        };
        Assert.IsGreaterThan(0d, Layout(data).Rows.Single().Width);
    }

    [TestMethod]
    public void LeapDayAndYearBoundaryUseCalendarDays()
    {
        var data = new CourtTimelineData
        {
            Start = new(2023, 12, 31), End = new(2024, 3, 1),
            Stages = [new() { Id = "leap", Start = new(2024, 2, 28), Closed = new(2024, 2, 29) }]
        };
        var layout = Layout(data);
        Assert.AreEqual(2 * layout.DayWidth, layout.Rows.Single().Width, 1e-8);
    }

    [TestMethod]
    public void StageAndEventCanHaveSameIdWithoutSelectionCollision()
    {
        var layout = Layout(Data(Event("stage", day: 30)));
        Assert.IsTrue(layout.Contains(new(CourtTimelineItemKind.Stage, "stage")));
        Assert.IsFalse(layout.Contains(new(CourtTimelineItemKind.Event, "stage")));
    }

    [TestMethod]
    public void InvalidRangesIdsAndReferencesAreRejected()
    {
        Assert.ThrowsExactly<ArgumentException>(() => Layout(Data() with { End = Date(1) }));
        Assert.ThrowsExactly<ArgumentException>(() => Layout(Data() with { Stages = [Stage() with { Deadline = Date(1) }] }));
        Assert.ThrowsExactly<ArgumentException>(() => Layout(Data() with { Stages = [Stage() with { Closed = Date(1) }] }));
        Assert.ThrowsExactly<ArgumentException>(() => Layout(Data() with { Stages = [Stage() with { PossibleFrom = Date(3) }] }));
        Assert.ThrowsExactly<ArgumentException>(() => Layout(Data() with { Stages = [Stage(), Stage()] }));
        Assert.ThrowsExactly<ArgumentException>(() => Layout(Data(Event("same"), Event("same"))));
        Assert.ThrowsExactly<ArgumentException>(() => Layout(Data(Event("orphan") with { StageId = "missing" })));
        Assert.ThrowsExactly<ArgumentException>(() => Layout(Data() with { Stages = [Stage() with { Id = " " }] }));
    }

    [TestMethod]
    public void NonFiniteGeometryIsRejected()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Layout(Data(), width: double.NaN));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Layout(Data(), zoom: double.PositiveInfinity));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Layout(Data(), zoom: 0));
    }
}
