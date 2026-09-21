# CourtTimeline.WinUI

[![CI](https://github.com/lawlabs/court-timeline-winui/actions/workflows/ci.yml/badge.svg)](https://github.com/lawlabs/court-timeline-winui/actions/workflows/ci.yml)

Standalone **WinUI 3** control for a court case timeline: stages, actual and planned periods, hearings and other events. The host application owns the data, storage and editing. There is no dependency on Kalends or LawMatic.

The library is extracted from the Windows `lawmatic-calendar-windows` prototype on `codex/court-timeline-demo`. It is a separate control, not a sixth `KalendsMode`.

## Features

- One row per court stage, with inclusive closing dates.
- Actual periods, hatched plans, dashed potential stages and a current-date marker.
- Stage/event selection, optional case header and details panel.
- Zoom, fit to viewport, horizontal/vertical scrolling and a plan toggle.
- Event labels use as many lanes as necessary to avoid overlaps.
- Light, dark and Windows high contrast; keyboard-focusable stage/event buttons.
- Russian and English UI strings, configurable date culture and resource overrides.
- Pure date/layout tests runnable on macOS, Linux and Windows.

## Requirements

Windows 10 1809 or later, .NET 10 SDK and Windows App SDK 2.5.1. Building the sample and running the UI require Windows. The project targets `net10.0-windows10.0.26100.0`, matching the Windows LawMatic/Kalends projects. The code-only library itself can be cross-compiled and packed with .NET 10 on macOS.

## Installation

Version `0.1.0` is a local development package; it has **not been published to NuGet**.

Use a project reference (adjust the relative path for your app):

```xml
<ProjectReference Include="..\court-timeline-winui\src\CourtTimeline.WinUI\CourtTimeline.WinUI.csproj" />
```

Or create and install a local package on Windows:

```powershell
dotnet pack src/CourtTimeline.WinUI/CourtTimeline.WinUI.csproj -c Release -o artifacts
dotnet add path/to/App.csproj package CourtTimeline.WinUI --version 0.1.0 --source ./artifacts
```

The control is built from native WinUI elements in C#. It has no library XAML/XBF, external theme dictionaries or embedded demo resources to copy into the consuming app. The app should initialize `XamlControlsResources` as in a standard WinUI application.

## Quick start

```xml
<Page
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:court="using:CourtTimeline">
    <court:CourtTimelineView x:Name="Timeline" />
</Page>
```

```csharp
using CourtTimeline;

Timeline.Data = new CourtTimelineData
{
    Number = "А40-87432/2026",
    Title = "Альфа → Вектор",
    Start = new DateOnly(2026, 4, 1),
    End = new DateOnly(2027, 1, 1), // exclusive axis boundary
    Stages = [new CourtStage
    {
        Id = "appeal", Title = "Апелляция", Court = "9-й арбитражный АС",
        State = CourtStageState.Active, Status = "В производстве",
        Start = new DateOnly(2026, 8, 4), End = new DateOnly(2026, 10, 16)
    }],
    Events = [new CourtEvent
    {
        Id = "hearing", StageId = "appeal", Title = "Судебное заседание",
        Date = new DateOnly(2026, 9, 29), Time = "11:00", Planned = true
    }]
};
Timeline.SelectionChanged += (_, args) =>
{
    // args.Event or args.Stage points to the host-supplied model.
    // A cleared selection has all three payload properties set to null.
};
Timeline.SelectedItem = new(CourtTimelineItemKind.Event, "hearing");
```

## API and behavior

| Property / method | Purpose |
| --- | --- |
| `Data` | A `CourtTimelineData` snapshot; null displays an empty state |
| `ShowPlan` | Show planned events and potential stages; true by default |
| `Zoom` / `FitToView()` | Scale 1–4; fit uses the available width, with a minimum of 480 DIPs |
| `SelectedItem` | `CourtTimelineSelection` with item kind and ID, or null |
| `SelectionChanged` | Selection plus the selected event and its parent stage, or the selected stage |
| `ShowCaseHeader`, `ShowDetails` | Hide built-in panels when the app supplies its own |
| `Culture`, `Strings` | Date formatting and visible UI text |
| `Refresh()` | Refresh after in-place collection changes or resource overrides |

`Data`, `ShowPlan`, `Zoom`, `SelectedItem`, `ShowCaseHeader` and `ShowDetails` are dependency properties and support binding. Set data before setting an initial selection. Use `Mode=TwoWay` if the view model should receive user changes to selection, zoom or the plan toggle. Invalid/hidden selections clear to null. Selections remain stable by kind and ID across redraws; stage and event IDs can coincide.

The records are snapshots with `IReadOnlyList` collections. Prefer assigning a new `Data` value after editing. The control does not subscribe to collection/item change notifications; call `Refresh()` for in-place changes. All control operations must run on the UI thread.

Dates use `DateOnly`: no time zone or daylight-saving arithmetic. `Data.End` is exclusive; `CourtStage.End` includes the complete last day. Stages/events outside the axis are clipped/omitted. `Data.Today` is an optional reference-date override for demos; null follows the local clock and updates once a minute while loaded. `Data.Now` places the today marker at that time of day. When both are null the marker follows the clock; when only `Today` is set the marker stays at midday. An active stage is factual through today and planned afterwards, bounded by its own dates. Completed stages are factual and potential stages are planned. Event `Planned` flags are supplied by the host, independently of the date. No legal deadlines are calculated.

Nonempty IDs must be unique within stages and within events, and every event must reference an existing stage. An inverted stage range, invalid axis, unknown enum value or invalid ID/reference throws `ArgumentException`. A null `Stages` or `Events` collection throws `ArgumentNullException`.

## Localization and theme

```csharp
Timeline.Culture = System.Globalization.CultureInfo.GetCultureInfo("en-US");
Timeline.Strings = CourtTimelineStrings.English;
// Or: CourtTimelineStrings.Russian with { Progress = "Движение дела" };
```

Override `CourtTimelineSurfaceBrush`, `CourtTimelineShellBrush`, `CourtTimelineTextBrush`, `CourtTimelineMutedBrush`, `CourtTimelineLineBrush`, `CourtTimelineSubtleLineBrush`, `CourtTimelineBrandBrush`, `CourtTimelineTodayBrush` and `CourtTimelineNowBrush` in the control or application resources. Theme dictionaries `Light`, `Dark`, `HighContrast` and direct resource entries are supported. All brushes have self-contained defaults. Stage tones are the `Blue`, `Green`, `Purple` palette.

## Sample and verification

A standalone [visual test app](samples/CourtTimeline.WinUI.Sample/README.md) is included. It has six data scenarios, theme/language selectors, header/details toggles, a reset button, and a selection status line.

On Windows, run it from the repository root:

```powershell
.\Run-Sample.ps1
```

Open `CourtTimeline.slnx` on Windows, or:

```powershell
dotnet test tests/CourtTimeline.WinUI.Tests/CourtTimeline.WinUI.Tests.csproj -c Release
dotnet build src/CourtTimeline.WinUI/CourtTimeline.WinUI.csproj -c Release
dotnet run --project samples/CourtTimeline.WinUI.Sample/CourtTimeline.WinUI.Sample.csproj -p:Platform=x64
```

The sample is an unpackaged app and needs the Windows App SDK runtime. The launcher detects x64/ARM64 automatically. When running dotnet directly on ARM64, pass `-p:Platform=ARM64 -r win-arm64`. Its fixture contains three stages and eight events with a fixed date of 19 September 2026; the library contains no fixture. CI also packs the library, builds the sample using the resulting NuGet package, and publishes a `CourtTimeline-Demo-win-x64` artifact with bundled runtimes.

See [architecture and extraction notes](docs/architecture.md) and [Windows UI verification](docs/windows-verification.md).

## License

MIT. Copyright © 2026 LawLabs (ООО «Лаборатория юридических исследований»).
