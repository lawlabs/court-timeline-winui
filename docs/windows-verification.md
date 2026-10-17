# Windows verification

Run on Windows with .NET 10 SDK, Windows App SDK 2.5.1 runtime, and current Windows SDK build tools.

```powershell
dotnet test tests/CourtTimeline.WinUI.Tests/CourtTimeline.WinUI.Tests.csproj -c Release
dotnet build src/CourtTimeline.WinUI/CourtTimeline.WinUI.csproj -c Release
dotnet pack src/CourtTimeline.WinUI/CourtTimeline.WinUI.csproj -c Release -o artifacts
dotnet build samples/CourtTimeline.WinUI.Sample/CourtTimeline.WinUI.Sample.csproj -c Release -p:Platform=x64
dotnet build samples/CourtTimeline.WinUI.Sample/CourtTimeline.WinUI.Sample.csproj -c Release -p:Platform=x64 -p:UsePackedCourtTimeline=true -p:RestoreAdditionalProjectSources="$((Resolve-Path artifacts).Path)"
dotnet run --project samples/CourtTimeline.WinUI.Sample/CourtTimeline.WinUI.Sample.csproj -p:Platform=x64
```

Start the visual test app with `.\Run-Sample.ps1` from the repository root. See [sample guide](../samples/CourtTimeline.WinUI.Sample/README.md) for the scenarios and a distributable Windows build.

Check the native sample:

1. Three stages/eight events are visible; the demo's selected appeal hearing has details.
2. Stage and event selection updates the status line, including when the built-in details panel is hidden; Tab and Enter/Space operate the same buttons.
3. «Без обещанного срока», «Срок ещё впереди», «Срок уже прошёл», «Стадия закрыта» and «Ещё не началась» show one geometry each: an open edge, a future deadline, an overdue deadline, a closed bar with a waiting window, and a potential stage.
4. Zoom and fit work at 700, 1000 and 1400 DIP widths; event labels do not overlap on a shared lane. Dense stages have at most three label lanes plus an overflow button, with a maximum height of 264 DIPs. Open the overflow menu and select an event: the details and host selection status must update.
5. System/light/dark selector, Windows high contrast, and theme changes while loaded preserve legibility and focus.
6. Select “Пустое дело” and “Нет выбранного дела”: the counts, case header and cleared details must match. Reset restores the first case and defaults. Switch to English to inspect localized UI and date formatting.
7. With the dense and year-boundary scenarios, inspect lane separation, the leap day and dates that cross the new year.
8. Unload/reload the control: timers and accessibility event handlers do not accumulate.
9. In LawMatic, `Ctrl+6` opens the extracted control and `Ctrl+1…5` return to calendar modes.
10. Select “109 событий за два года”: the open stage has no closing date or deadline. Verify the total is 109, resize the window, select visible and overflow events, and leave the app idle across a minute tick. It must remain responsive without continuously rebuilding its canvas or accumulating memory. Repeat after hiding/showing the timeline and toggling header/details.

Cross-platform tests exercise the real calendar/layout code, including a leap day, inclusive closed and deadline days, axis clipping, open and overdue stages, future active stages, hidden selections, dense events, empty input, invalid IDs and DateOnly limits. They do not verify native rendering or replace the Windows checks above.

## Verified on 2026-10-17

32 pure layout/cache tests pass. The standalone sample and the direct LawMatic.Calendar consumer build in Debug x64 with no warnings/errors. The sample launches with a real native window; automating its scenario picker encountered a WinApp UIA RuntimeId mismatch, so dense/overflow interaction verification was performed in CaseTracker instead.

CaseTracker Debug/Release builds succeed, and its 28 native UI regression checks pass, including Standard/CaseTracker/LegalCalc palettes and light/dark changes. An isolated saved portfolio measured one rebuild per selection: a case with 4 stages/109 events fell from five synchronous builds totalling 22.5 seconds to roughly 34-40 ms to the next CompositionTarget.Rendering in warm Debug runs (133 ms on the first recorded run). A sparse case takes 15-18 ms. The benchmark is specific to this machine/data and is not a cold-start, huge-portfolio or memory-leak guarantee. Overflow events and repeat event details are tested in the host. The reproducible harness and detailed evidence notes live in casetracker-winui/scripts/Test-TimelinePerformance.ps1 and docs/timeline-performance.md.
