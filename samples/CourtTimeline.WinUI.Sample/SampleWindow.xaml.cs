using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;

namespace CourtTimeline.Sample;

public sealed partial class SampleWindow : Window
{
    private bool _ready;

    public SampleWindow()
    {
        InitializeComponent();
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
        Timeline.SelectedItem = null;
        Timeline.ShowPlan = true;
        Timeline.Data = scenario.Create();
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

    private void Reset_Click(object sender, RoutedEventArgs args)
    {
        // Explicitly restore even if the first scenario was already selected.
        _ready = false;
        ScenarioPicker.SelectedIndex = 0;
        ThemePicker.SelectedIndex = 0;
        LanguagePicker.SelectedIndex = 0;
        HeaderToggle.IsChecked = true;
        DetailsToggle.IsChecked = true;
        _ready = true;
        ApplyTheme();
        ApplyLanguage();
        ApplyPanels();
        ApplyScenario();
    }
}
