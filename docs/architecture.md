# Architecture and extraction

`src/CourtTimeline.WinUI` produces one NuGet package, `CourtTimeline.WinUI`, with namespace `CourtTimeline`. There is no reference to Kalends, the LawMatic executable, Legalic or any persistence layer.

- `Public/CourtTimelineData.cs`: host-owned stage/event snapshots, enums and selection event payloads.
- `Public/CourtTimelineView.cs`: bindable public API, lifetime and refresh handling.
- `Public/CourtTimelineStrings.cs`: replaceable Russian/English UI strings.
- `Internal/TimelineLayout.cs`: calendar arithmetic, clipping and collision-free event lanes, without WinUI dependencies.
- `Internal/CourtTimelineView.Rendering.cs`: native WinUI elements, interactions and details.
- `Internal/TimelineTheme.cs`: private default palette and prefixed host resource overrides.
- `samples`: standalone executable, fixed demo fixture and source-generated JSON metadata.
- `tests`: links the actual pure model/layout source for cross-platform tests without loading WinUI.

The source is the native Windows prototype in `lawmatic-calendar-windows`, branch `codex/court-timeline-demo`, not the HTML preview. The Apple checkout was inspected and contains no court timeline; the scope was confirmed as Windows/WinUI 3 only.

The original prototype owned a fixed JSON fixture, depended on application brush keys, assumed 2026 and one active stage, used a two-lane label arrangement and a 960 DIP minimum even in fit mode. Extraction removes those assumptions. Metadata, range, reference date and labels are now inputs; event lanes grow as needed; clipping handles empty, future and out-of-range data. This is a read-only visualization with host-managed data updates, not an editor or deadline engine.

LawMatic's demo branch now references the sibling `court-timeline-winui` project. The JSON fixture and adapter remain in LawMatic; the old app-local control is removed. Kalends remains responsible for its calendar modes.

Rendering uses code-created WinUI controls, so the package requires no XBF files or custom dictionary deployment. The sample's `App.xaml` demonstrates normal application-level `XamlControlsResources` setup. See Microsoft's [WinUI class library guidance](https://learn.microsoft.com/en-us/windows/apps/tutorials/winui-mvvm-toolkit/class-library).

This first version renders the complete visible-range canvas, without virtualization. Very large histories or thousands of same-day events should be narrowed with `Data.Start`/`End`; large-scale virtualization is outside this extraction.
