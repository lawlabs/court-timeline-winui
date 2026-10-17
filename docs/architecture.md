# Architecture and extraction

`src/CourtTimeline.WinUI` produces one NuGet package, `CourtTimeline.WinUI`, with namespace `CourtTimeline`. There is no reference to Kalends, the LawMatic executable, Legalic or any persistence layer.

- `Public/CourtTimelineData.cs`: host-owned stage/event snapshots, enums and selection event payloads.
- `Public/CourtTimelineView.cs`: bindable public API, lifetime and refresh handling.
- `Public/CourtTimelineStrings.cs`: replaceable Russian/English UI strings.
- `Internal/TimelineLayout.cs`: calendar arithmetic, clipping and bounded collision-free event lanes, without WinUI dependencies.
- `Internal/TimelineLayoutCache.cs`: measured-width guard and geometry reuse by data identity, effective viewport width, zoom and calendar day; explicit invalidation supports in-place data updates.
- `Internal/CourtTimelineView.Rendering.cs`: native WinUI elements, interactions and details.
- `Internal/TimelineTheme.cs`: private default palette and prefixed host resource overrides.
- `samples`: standalone executable, fixed demo fixture and source-generated JSON metadata.
- `tests`: links the actual pure model/layout source for cross-platform tests without loading WinUI.

The source is the native Windows prototype in `lawmatic-calendar-windows`, branch `codex/court-timeline-demo`, not the HTML preview. The Apple checkout was inspected and contains no court timeline; the scope was confirmed as Windows/WinUI 3 only.

The original prototype owned a fixed JSON fixture, depended on application brush keys, assumed 2026 and one active stage, used a two-lane label arrangement and a 960 DIP minimum even in fit mode. Extraction removes those assumptions. Metadata, range, reference date and labels are now inputs; up to three label lanes are followed by a selectable overflow menu; clipping handles empty, future and out-of-range data. This is a read-only visualization with host-managed data updates, not an editor or deadline engine.

LawMatic's demo branch now references the sibling `court-timeline-winui` project. The JSON fixture and adapter remain in LawMatic; the old app-local control is removed. Kalends remains responsible for its calendar modes.

Rendering uses code-created WinUI controls, so the package requires no XBF files or custom dictionary deployment. The sample's `App.xaml` demonstrates normal application-level `XamlControlsResources` setup. See Microsoft's [WinUI class library guidance](https://learn.microsoft.com/en-us/windows/apps/tutorials/winui-mvvm-toolkit/class-library).

The canvas renders the selected date range without viewport virtualization. Each stage is bounded to 264 DIPs, and overflow menu items are created only when requested. Width/height notifications do not invalidate geometry; the control compares the measured viewport width and ignores sub-DIP jitter. Selection changes update existing buttons and details, and minute ticks update the clock caption without recreating the canvas. Theme and localization changes redraw visuals using cached geometry. `Refresh()` explicitly invalidates geometry for host-owned mutable collections and resource overrides. For very large numbers of stages, narrowing `Data.Start`/`End` is still useful; large-scale virtualization remains outside this component.

## UI update cost (2026-10-17)

Data/zoom/shell/theme notifications are coalesced with DispatcherQueue. Selection updates existing visuals and details synchronously before notifying the host. Per-control brush caching is invalidated by ActualThemeChanged, HighContrastChanged and Refresh; missing CourtTimeline resource keys never enumerate deferred framework resources. Hidden headers/details do not construct their contents. Overflow date dots and hatching use batched geometries, retaining every event in the model and overflow menu.

`GetSelectionAnchor(selection)` returns a native event/stage button, or the overflow button for an event without a visible label. Hosts can anchor their own flyout without walking the visual tree. `RenderCompleted` is optional diagnostics: Elapsed measures this control's UI update, excluding subsequent XAML layout and presentation. No stopwatch or event payload is created without subscribers. Setting several presentation properties is deferred/coalesced; use SelectionChanged and GetSelectionAnchor when the rendered selection target is needed. CompactMode defaults to false and hides duplicate chrome when embedded in a case pane. Existing zoom AutomationIds remain CourtTimeline.Fit/ZoomIn/ZoomOut.

CaseTracker's application-level fix is also necessary: publish one stable snapshot after selecting a case, reuse it across tab/stage switches, and provide a new snapshot (or call Refresh) after updating its contents. The library cannot eliminate repeated host-side projection work.
