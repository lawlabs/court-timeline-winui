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
4. Zoom and fit work at 700, 1000 and 1400 DIP widths; event labels do not overlap on a shared lane. Dense data grows rows vertically.
5. System/light/dark selector, Windows high contrast, and theme changes while loaded preserve legibility and focus.
6. Select “Пустое дело” and “Нет выбранного дела”: the counts, case header and cleared details must match. Reset restores the first case and defaults. Switch to English to inspect localized UI and date formatting.
7. With the dense and year-boundary scenarios, inspect lane separation, the leap day and dates that cross the new year.
8. Unload/reload the control: timers and accessibility event handlers do not accumulate.
9. In LawMatic, `Ctrl+6` opens the extracted control and `Ctrl+1…5` return to calendar modes.

Cross-platform tests exercise the real calendar/layout code, including a leap day, inclusive closed and deadline days, axis clipping, open and overdue stages, future active stages, hidden selections, dense events, empty input, invalid IDs and DateOnly limits. They do not verify native rendering or replace the Windows checks above.
