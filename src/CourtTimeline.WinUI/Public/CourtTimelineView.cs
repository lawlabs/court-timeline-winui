using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Windows.UI.ViewManagement;

namespace CourtTimeline;

/// <summary>A presentation-only court process timeline. The host owns data, persistence and editing.</summary>
public sealed partial class CourtTimelineView : UserControl
{
    public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
        nameof(Data), typeof(object), typeof(CourtTimelineView), new PropertyMetadata(null, VisualChanged));
    public static readonly DependencyProperty ShowPlanProperty = DependencyProperty.Register(
        nameof(ShowPlan), typeof(bool), typeof(CourtTimelineView), new PropertyMetadata(true, VisualChanged));
    public static readonly DependencyProperty ZoomProperty = DependencyProperty.Register(
        nameof(Zoom), typeof(double), typeof(CourtTimelineView), new PropertyMetadata(1d, VisualChanged));
    public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
        nameof(SelectedItem), typeof(object), typeof(CourtTimelineView), new PropertyMetadata(null, SelectionChangedCallback));
    public static readonly DependencyProperty ShowCaseHeaderProperty = DependencyProperty.Register(
        nameof(ShowCaseHeader), typeof(bool), typeof(CourtTimelineView), new PropertyMetadata(true, VisualChanged));
    public static readonly DependencyProperty ShowDetailsProperty = DependencyProperty.Register(
        nameof(ShowDetails), typeof(bool), typeof(CourtTimelineView), new PropertyMetadata(true, VisualChanged));

    public CourtTimelineData? Data { get => GetValue(DataProperty) as CourtTimelineData; set => SetValue(DataProperty, value); }
    public bool ShowPlan { get => (bool)GetValue(ShowPlanProperty); set => SetValue(ShowPlanProperty, value); }
    /// <summary>Horizontal scale in [1,4]. One fits the viewport (minimum content width 480 DIPs).</summary>
    public double Zoom { get => (double)GetValue(ZoomProperty); set => SetValue(ZoomProperty, value); }
    public CourtTimelineSelection? SelectedItem { get => GetValue(SelectedItemProperty) as CourtTimelineSelection; set => SetValue(SelectedItemProperty, value); }
    public bool ShowCaseHeader { get => (bool)GetValue(ShowCaseHeaderProperty); set => SetValue(ShowCaseHeaderProperty, value); }
    public bool ShowDetails { get => (bool)GetValue(ShowDetailsProperty); set => SetValue(ShowDetailsProperty, value); }
    public CultureInfo Culture { get => _culture; set { _culture = value ?? throw new ArgumentNullException(nameof(value)); Refresh(); } }
    public CourtTimelineStrings Strings { get => _strings; set { _strings = value ?? throw new ArgumentNullException(nameof(value)); Refresh(); } }

    public event EventHandler<CourtTimelineSelectionChangedEventArgs>? SelectionChanged;

    private CultureInfo _culture = CultureInfo.GetCultureInfo("ru-RU");
    private CourtTimelineStrings _strings = CourtTimelineStrings.Russian;
    private readonly Grid _root = new() { RowSpacing = 16, Padding = new Thickness(24, 6, 24, 16) };
    private readonly Grid _header = new() { ColumnSpacing = 16 };
    private readonly StackPanel _case = new() { Spacing = 5 };
    private readonly StackPanel _amount = new() { Spacing = 5, HorizontalAlignment = HorizontalAlignment.Right };
    private readonly Grid _toolbar = new() { ColumnSpacing = 12, RowSpacing = 8 };
    private readonly StackPanel _summary = new() { Orientation = Orientation.Horizontal, Spacing = 12 };
    private readonly StackPanel _actions = new() { Orientation = Orientation.Horizontal, Spacing = 4 };
    private readonly ToggleButton _plan = new() { FontSize = 11, Padding = new Thickness(10, 5, 10, 5) };
    private readonly Button _zoomOut = new() { Content = "−" };
    private readonly Button _zoomIn = new() { Content = "+" };
    private readonly Button _fit = new() { FontSize = 11 };
    private readonly Border _frame = new() { BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8) };
    private readonly ScrollViewer _scroller = new()
    {
        HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollMode = ScrollMode.Enabled,
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto, ZoomMode = ZoomMode.Disabled
    };
    private readonly Canvas _canvas = new();
    private readonly TextBlock _legend = new() { FontSize = 11, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(14, 9, 14, 9) };
    private readonly Border _details = new() { Padding = new Thickness(16, 12, 16, 12), CornerRadius = new CornerRadius(8), BorderThickness = new Thickness(1) };
    private readonly AccessibilitySettings _accessibility = new();
    private readonly DispatcherTimer _clock = new() { Interval = TimeSpan.FromMinutes(1) };
    private TimelineLayout.Result? _layout;
    private CourtTimelineSelection? _notifiedSelection;
    private bool _ready, _rendering, _queued, _subscribed, _highContrast;

    public CourtTimelineView()
    {
        IsTabStop = false;
        for (int i = 0; i < 4; i++) _root.RowDefinitions.Add(new() { Height = i == 2 ? new GridLength(1, GridUnitType.Star) : GridLength.Auto });
        _header.ColumnDefinitions.Add(new());
        _header.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        _header.Children.Add(_case);
        Grid.SetColumn(_amount, 1); _header.Children.Add(_amount);
        _toolbar.ColumnDefinitions.Add(new());
        _toolbar.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        _toolbar.RowDefinitions.Add(new() { Height = GridLength.Auto });
        _toolbar.RowDefinitions.Add(new() { Height = GridLength.Auto });
        _toolbar.Children.Add(_summary);
        _actions.Children.Add(_plan); _actions.Children.Add(_zoomOut); _actions.Children.Add(_fit); _actions.Children.Add(_zoomIn);
        _toolbar.Children.Add(_actions);
        _scroller.Content = _canvas;
        var timeline = new Grid();
        timeline.RowDefinitions.Add(new()); timeline.RowDefinitions.Add(new() { Height = GridLength.Auto });
        timeline.Children.Add(_scroller); Grid.SetRow(_legend, 1); timeline.Children.Add(_legend);
        _frame.Child = timeline;
        AddRow(_header, 0); AddRow(_toolbar, 1); AddRow(_frame, 2); AddRow(_details, 3);
        Content = _root;
        _plan.Click += (_, _) => ShowPlan = _plan.IsChecked == true;
        _zoomIn.Click += (_, _) => Zoom = Math.Min(4, Zoom + .5);
        _zoomOut.Click += (_, _) => Zoom = Math.Max(1, Zoom - .5);
        _fit.Click += (_, _) => FitToView();
        _scroller.SizeChanged += (_, _) => QueueRefresh();
        ActualThemeChanged += (_, _) => Refresh();
        _clock.Tick += (_, _) => { if (Data?.Today is null) Refresh(); };
        Loaded += (_, _) =>
        {
            if (!_subscribed)
            {
                try
                {
                    _accessibility.HighContrastChanged += HighContrastChanged;
                    _subscribed = true;
                }
                catch (System.Runtime.InteropServices.COMException)
                {
                    // HighContrastChanged is unavailable in some unpackaged hosts.
                }
            }
            _clock.Start(); Refresh();
        };
        Unloaded += (_, _) =>
        {
            _clock.Stop();
            if (_subscribed)
            {
                _accessibility.HighContrastChanged -= HighContrastChanged;
                _subscribed = false;
            }
        };
        _ready = true;
    }

    private void AddRow(FrameworkElement element, int row) { Grid.SetRow(element, row); _root.Children.Add(element); }
    private void HighContrastChanged(AccessibilitySettings sender, object args) => QueueRefresh();
    private void QueueRefresh()
    {
        if (_queued) return;
        _queued = true;
        if (!DispatcherQueue.TryEnqueue(() => { _queued = false; Refresh(); })) _queued = false;
    }
    private static void VisualChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args) => ((CourtTimelineView)sender).Refresh();
    private static void SelectionChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var view = (CourtTimelineView)sender;
        view.Refresh();
    }

    private void NotifySelection()
    {
        var selection = SelectedItem;
        if (selection == _notifiedSelection) return;
        _notifiedSelection = selection;
        var item = selection?.Kind == CourtTimelineItemKind.Event ? Data?.Events.FirstOrDefault(e => e.Id == selection.Id) : null;
        var stageId = item?.StageId ?? (selection?.Kind == CourtTimelineItemKind.Stage ? selection.Id : null);
        var stage = Data?.Stages.FirstOrDefault(s => s.Id == stageId);
        SelectionChanged?.Invoke(this, new(selection, stage, item));
    }

    /// <summary>Rebuild after changing a collection in place or resource overrides. Use on the UI thread.</summary>
    public void Refresh()
    {
        if (!_ready || _rendering) return;
        _rendering = true;
        try
        {
            if (!double.IsFinite(Zoom) || Zoom is < 1 or > 4) Zoom = double.IsFinite(Zoom) ? Math.Clamp(Zoom, 1, 4) : 1;
            _highContrast = _accessibility.HighContrast;
            var focus = XamlRoot is null ? null : FocusManager.GetFocusedElement(XamlRoot) as Button;
            var focusSelection = focus is not null && _canvas.Children.Contains(focus) ? focus.Tag as CourtTimelineSelection : null;
            var focusState = focus?.FocusState ?? FocusState.Unfocused;
            _layout = Data is { } data ? TimelineLayout.Calculate(data, TodayMark(data).Date, ShowPlan,
                Math.Max(1, _scroller.ViewportWidth > 0 ? _scroller.ViewportWidth : ActualWidth - 50), Zoom) : null;
            if (SelectedItem is { } selection && (_layout is null || !_layout.Contains(selection))) SelectedItem = null;
            RenderShell();
            _canvas.Children.Clear();
            _canvas.Background = Brush("Surface");
            _canvas.Width = _layout?.Width ?? Math.Max(480, ActualWidth - 50);
            _canvas.Height = _layout?.Height ?? 200;
            if (Data is not null && _layout is not null) RenderTimeline(Data, _layout);
            if (_layout is null || _layout.Rows.Count == 0) Put(Text(Strings.Empty, 13, "Muted"), 20, 95);
            RenderDetails();
            if (focusSelection is not null && focusState != FocusState.Unfocused)
            {
                var target = _canvas.Children.OfType<Button>().FirstOrDefault(b => Equals(b.Tag, focusSelection));
                if (target is not null) target.Focus(focusState);
                else _plan.Focus(focusState);
            }
        }
        finally { _rendering = false; }
        // Notify after drawing so a host can safely replace Data from the event handler.
        NotifySelection();
    }

    public void FitToView() { Zoom = 1; Refresh(); _scroller.ChangeView(0, 0, null); }
}
