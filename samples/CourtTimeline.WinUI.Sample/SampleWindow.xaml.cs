using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;

namespace CourtTimeline.Sample;

public sealed partial class SampleWindow : Window
{
    private readonly DispatcherTimer _nowClock = new() { Interval = TimeSpan.FromMinutes(1) };
    private bool _ready;
    private bool _applyingNow;

    public SampleWindow()
    {
        InitializeComponent();
        _nowClock.Tick += (_, _) => { if (LiveNowToggle.IsChecked == true) ShowClock(); };
        ScenarioPicker.ItemsSource = DemoScenarios.All.Select(scenario => scenario.Title).ToArray();
        Timeline.SelectionChanged += (_, args) =>
        {
            SelectionStatus.Text = args.Event is { } item
                ? $"Событие: {item.Title} · {item.Date.ToString("d MMM yyyy", Timeline.Culture)}"
                : args.Stage is { } stage ? $"Стадия: {stage.Title} · {stage.Status}" : "Ничего не выбрано";
        };
        _ready = true;
        ScenarioPicker.SelectedIndex = 0;
        AppWindow.Resize(new SizeInt32(1280, 960));
    }

    private void Scenario_Changed(object sender, SelectionChangedEventArgs args) => ApplyScenario();

    private void ApplyScenario()
    {
        if (!_ready || ScenarioPicker.SelectedIndex < 0) return;
        var scenario = DemoScenarios.All[ScenarioPicker.SelectedIndex];
        var data = scenario.Create();
        if (LiveNowToggle.IsChecked == true) ShowClock();
        else ShowScenarioDate(data);
        Timeline.SelectedItem = null;
        Timeline.ShowPlan = true;
        Timeline.Data = WithNow(data);
        ScenarioNote.Text = scenario.Description;
        Timeline.FitToView();
        Timeline.SelectedItem = scenario.InitialSelection;
    }

    private void Theme_Changed(object sender, SelectionChangedEventArgs args) => ApplyTheme();

    private void ApplyTheme()
    {
        if (!_ready) return;
        Root.RequestedTheme = ThemePicker.SelectedIndex switch
        {
            1 => ElementTheme.Light,
            2 => ElementTheme.Dark,
            _ => ElementTheme.Default
        };
    }

    private void Language_Changed(object sender, SelectionChangedEventArgs args) => ApplyLanguage();

    private void ApplyLanguage()
    {
        if (!_ready) return;
        bool english = LanguagePicker.SelectedIndex == 1;
        Timeline.Culture = CultureInfo.GetCultureInfo(english ? "en-US" : "ru-RU");
        Timeline.Strings = english ? CourtTimelineStrings.English : CourtTimelineStrings.Russian;
        // The demo records remain in their authored language; only the control UI is localized.
    }

    private void Panels_Changed(object sender, RoutedEventArgs args) => ApplyPanels();

    private void ApplyPanels()
    {
        if (!_ready) return;
        Timeline.ShowCaseHeader = HeaderToggle.IsChecked == true;
        Timeline.ShowDetails = DetailsToggle.IsChecked == true;
    }

    private void LiveNow_Changed(object sender, RoutedEventArgs args) => ApplyNow();

    private void NowDate_Changed(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args) => ApplyNow();

    private void NowTime_Changed(TimePicker sender, TimePickerSelectedValueChangedEventArgs args) => ApplyNow();

    private void ApplyNow()
    {
        if (!_ready || _applyingNow) return;
        if (LiveNowToggle.IsChecked == true)
        {
            ShowClock();
            _nowClock.Start();
        }
        else
        {
            _nowClock.Stop();
            NowDate.IsEnabled = NowTime.IsEnabled = true;
        }
        if (Timeline.Data is { } data) Timeline.Data = WithNow(data);
    }

    private CourtTimelineData? WithNow(CourtTimelineData? data)
    {
        if (data is null) return null;
        if (LiveNowToggle.IsChecked == true) return data with { Today = null, Now = null };
        return data with { Today = PickedDate(), Now = PickedTime() };
    }

    private void ShowClock()
    {
        var now = DateTime.Now;
        ShowPickers(DateOnly.FromDateTime(now), now.TimeOfDay, enabled: false);
    }

    private void ShowScenarioDate(CourtTimelineData? data)
    {
        var date = data?.Today ?? DateOnly.FromDateTime(DateTime.Now);
        ShowPickers(date, TimeSpan.FromHours(12), enabled: true);
    }

    private void ShowPickers(DateOnly date, TimeSpan time, bool enabled)
    {
        _applyingNow = true;
        NowDate.Date = date.ToDateTime(TimeOnly.MinValue);
        NowTime.SelectedTime = time;
        NowDate.IsEnabled = NowTime.IsEnabled = enabled;
        _applyingNow = false;
    }

    private DateOnly PickedDate() => NowDate.Date is { } value
        ? DateOnly.FromDateTime(value.LocalDateTime)
        : DateOnly.FromDateTime(DateTime.Today);

    private TimeOnly? PickedTime() => NowTime.SelectedTime is { } time && time < TimeSpan.FromDays(1)
        ? TimeOnly.FromTimeSpan(time)
        : null;

    private void Reset_Click(object sender, RoutedEventArgs args)
    {
        // Explicitly restore even if the first scenario was already selected.
        _nowClock.Stop();
        _ready = false;
        ScenarioPicker.SelectedIndex = 0;
        ThemePicker.SelectedIndex = 0;
        LanguagePicker.SelectedIndex = 0;
        HeaderToggle.IsChecked = true;
        DetailsToggle.IsChecked = true;
        LiveNowToggle.IsChecked = false;
        _ready = true;
        ApplyTheme();
        ApplyLanguage();
        ApplyPanels();
        ApplyScenario();
    }
}
