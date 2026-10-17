using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.UI.ViewManagement;

namespace CourtTimeline;

/// <summary>A presentation-only court process timeline. The host owns data, persistence and editing.</summary>
public sealed partial class CourtTimelineView : UserControl
{
    public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
        nameof(Data), typeof(object), typeof(CourtTimelineView), new PropertyMetadata(null, VisualChanged));
    public static readonly DependencyProperty ZoomProperty = DependencyProperty.Register(
        nameof(Zoom), typeof(double), typeof(CourtTimelineView), new PropertyMetadata(1d, VisualChanged));
    public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
        nameof(SelectedItem), typeof(object), typeof(CourtTimelineView), new PropertyMetadata(null, SelectionChangedCallback));
    public static readonly DependencyProperty ShowCaseHeaderProperty = DependencyProperty.Register(
        nameof(ShowCaseHeader), typeof(bool), typeof(CourtTimelineView), new PropertyMetadata(true, ShellChanged));
    public static readonly DependencyProperty ShowDetailsProperty = DependencyProperty.Register(
        nameof(ShowDetails), typeof(bool), typeof(CourtTimelineView), new PropertyMetadata(true, ShellChanged));
    public static readonly DependencyProperty CompactModeProperty = DependencyProperty.Register(
        nameof(CompactMode), typeof(bool), typeof(CourtTimelineView), new PropertyMetadata(false, ShellChanged));

    public CourtTimelineData? Data { get => GetValue(DataProperty) as CourtTimelineData; set => SetValue(DataProperty, value); }
    /// <summary>Horizontal scale in [1,4]. One fits the viewport (minimum content width 480 DIPs).</summary>
    public double Zoom { get => (double)GetValue(ZoomProperty); set => SetValue(ZoomProperty, value); }
    public CourtTimelineSelection? SelectedItem { get => GetValue(SelectedItemProperty) as CourtTimelineSelection; set => SetValue(SelectedItemProperty, value); }
    public bool ShowCaseHeader { get => (bool)GetValue(ShowCaseHeaderProperty); set => SetValue(ShowCaseHeaderProperty, value); }
    public bool ShowDetails { get => (bool)GetValue(ShowDetailsProperty); set => SetValue(ShowDetailsProperty, value); }
    /// <summary>Reduce surrounding chrome when hosted inside an existing case detail pane.</summary>
    public bool CompactMode { get => (bool)GetValue(CompactModeProperty); set => SetValue(CompactModeProperty, value); }
    public CultureInfo Culture { get => _culture; set { _culture = value ?? throw new ArgumentNullException(nameof(value)); InvalidateVisuals(); } }
    public CourtTimelineStrings Strings { get => _strings; set { _strings = value ?? throw new ArgumentNullException(nameof(value)); InvalidateVisuals(); } }

    public event EventHandler<CourtTimelineSelectionChangedEventArgs>? SelectionChanged;
    /// <summary>Opt-in timing of UI updates. No timer or event arguments are allocated without a subscriber.</summary>
    public event EventHandler<CourtTimelineRenderCompletedEventArgs>? RenderCompleted;

    private CultureInfo _culture = CultureInfo.GetCultureInfo("ru-RU");
    private CourtTimelineStrings _strings = CourtTimelineStrings.Russian;
    private readonly Grid _root = new() { RowSpacing = 16, Padding = new Thickness(24, 6, 24, 16) };
    private readonly Grid _header = new() { ColumnSpacing = 16 };
    private readonly StackPanel _case = new() { Spacing = 5 };
    private readonly StackPanel _amount = new() { Spacing = 5, HorizontalAlignment = HorizontalAlignment.Right };
    private readonly Grid _toolbar = new() { ColumnSpacing = 12, RowSpacing = 8 };
    private readonly StackPanel _summary = new() { Orientation = Orientation.Horizontal, Spacing = 12 };
    private readonly StackPanel _actions = new() { Orientation = Orientation.Horizontal, Spacing = 4 };
    private readonly Button _zoomOut = new() { Content = "−" };
    private readonly Button _zoomIn = new() { Content = "+" };
    private readonly Button _fit = new() { FontSize = 11 };
    private readonly Border _frame = new() { BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8) };
    private readonly ScrollViewer _scroller = new()
    {
        HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollMode = ScrollMode.Enabled,
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto, ZoomMode = ZoomMode.Disabled,
        VerticalContentAlignment = VerticalAlignment.Top, HorizontalContentAlignment = HorizontalAlignment.Left
    };
    private readonly Canvas _canvas = new() { VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = HorizontalAlignment.Left };
    private readonly TextBlock _legend = new() { FontSize = 11, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(14, 9, 14, 9) };
    private readonly Border _details = new() { Padding = new Thickness(16, 12, 16, 12), CornerRadius = new CornerRadius(8), BorderThickness = new Thickness(1) };
    private readonly AccessibilitySettings _accessibility = new();
    private readonly DispatcherTimer _clock = new() { Interval = TimeSpan.FromMinutes(1) };
    private TimelineLayout.Result? _layout;
    private readonly TimelineLayoutCache _layoutCache = new();
    private CourtTimelineSelection? _notifiedSelection;
    private bool _ready, _rendering, _queued, _subscribed, _highContrast;
    private bool _visualsDirty = true, _shellDirty = true, _selectionDirty = true;

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
        _actions.Children.Add(_zoomOut); _actions.Children.Add(_fit); _actions.Children.Add(_zoomIn);
        _toolbar.Children.Add(_actions);
        _scroller.Content = _canvas;
        var timeline = new Grid();
        timeline.RowDefinitions.Add(new()); timeline.RowDefinitions.Add(new() { Height = GridLength.Auto });
        timeline.Children.Add(_scroller); Grid.SetRow(_legend, 1); timeline.Children.Add(_legend);
        _frame.Child = timeline;
        AddRow(_header, 0); AddRow(_toolbar, 1); AddRow(_frame, 2); AddRow(_details, 3);
        Content = _root;
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(_zoomOut, "CourtTimeline.ZoomOut");
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(_zoomIn, "CourtTimeline.ZoomIn");
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(_fit, "CourtTimeline.Fit");
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(_scroller, "TimelineViewport");
        _zoomIn.Click += (_, _) => Zoom = Math.Min(4, Zoom + .5);
        _zoomOut.Click += (_, _) => Zoom = Math.Max(1, Zoom - .5);
        _fit.Click += (_, _) => FitToView();
        _scroller.SizeChanged += (_, args) =>
        {
            if (args.NewSize.Width != args.PreviousSize.Width) QueueRefresh();
        };
        _scroller.RegisterPropertyChangedCallback(ScrollViewer.ViewportWidthProperty, (_, _) => QueueRefresh());
        ActualThemeChanged += (_, _) => InvalidateVisuals();
        _clock.Tick += (_, _) => { if (Data?.Today is null) RefreshCore(); };
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
            _clock.Start(); InvalidateVisuals(); QueueRefresh();
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
    private void HighContrastChanged(AccessibilitySettings sender, object args)
    {
        DispatcherQueue.TryEnqueue(InvalidateVisuals);
    }
    private void InvalidateVisuals()
    {
        _brushes.Clear();
        _visualsDirty = _shellDirty = true;
        QueueRefresh();
    }
    private void QueueRefresh()
    {
        if (_queued) return;
        _queued = true;
        if (!DispatcherQueue.TryEnqueue(() => { _queued = false; RefreshCore(); })) _queued = false;
    }
    private static void VisualChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args) => ((CourtTimelineView)sender).QueueRefresh();
    private static void ShellChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var view = (CourtTimelineView)sender;
        view._shellDirty = true;
        view.QueueRefresh();
    }
    private static void SelectionChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var view = (CourtTimelineView)sender;
        view._selectionDirty = true;
        view.RefreshCore();
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

    /// <summary>The rendered native button for a selection, or its overflow button for an event without a visible label.</summary>
    public Button? GetSelectionAnchor(CourtTimelineSelection selection)
    {
        foreach (var visual in _selectionVisuals)
            if (visual.Selection == selection) return visual.Button;
        if (selection.Kind == CourtTimelineItemKind.Event)
            foreach (var (button, row) in _overflowButtons)
                if (row.OverflowEvents.Any(item => item.Id == selection.Id)) return button;
        return null;
    }

    /// <summary>Rebuild after changing a collection in place or resource overrides. Use on the UI thread.</summary>
    public void Refresh()
    {
        _layoutCache.Invalidate();
        InvalidateVisuals();
    }

    private void RefreshCore()
    {
        if (!_ready) return;
        if (_rendering) { QueueRefresh(); return; }
        var diagnostics = RenderCompleted;
        long started = diagnostics is null ? 0 : System.Diagnostics.Stopwatch.GetTimestamp();
        bool rebuilt = false;
        _rendering = true;
        try
        {
            if (!double.IsFinite(Zoom) || Zoom is < 1 or > 4) Zoom = double.IsFinite(Zoom) ? Math.Clamp(Zoom, 1, 4) : 1;
            _highContrast = _accessibility.HighContrast;
            double viewportWidth = _scroller.ViewportWidth;
            if (Data is not null && (!IsLoaded || !double.IsFinite(viewportWidth) || viewportWidth <= 0))
            {
                // Render the shell once so it can be measured, but never invent a viewport.
                if (_shellDirty) { _shellDirty = false; RenderShell(); }
                return;
            }
            bool geometryChanged = _layoutCache.Update(Data, Data is { } data ? TodayMark(data).Date : default, viewportWidth, Zoom);
            _layout = _layoutCache.Result;
            if (SelectedItem is { } selection && (_layout is null || !_layout.Contains(selection))) SelectedItem = null;
            bool rebuild = geometryChanged || _visualsDirty;
            bool shellChanged = rebuild || _shellDirty;
            bool selectionChanged = rebuild || _selectionDirty;
            _visualsDirty = _shellDirty = _selectionDirty = false;
            if (shellChanged) RenderShell();
            if (rebuild)
            {
                rebuilt = true;
                var focus = XamlRoot is null ? null : FocusManager.GetFocusedElement(XamlRoot) as Button;
                var focusTag = focus is not null && _canvas.Children.Contains(focus) ? focus.Tag : null;
                var focusState = focus?.FocusState ?? FocusState.Unfocused;
                _canvas.Children.Clear();
                _selectionVisuals.Clear();
                _overflowButtons.Clear();
                _todayLabel = null;
                _canvas.Background = Brush("Surface");
                _canvas.Width = _layout?.Width ?? (double.IsFinite(viewportWidth) && viewportWidth > 0
                    ? Math.Round(Math.Max(480, viewportWidth)) : 0);
                _canvas.Height = _layout?.Height ?? 200;
                if (Data is not null && _layout is not null) RenderTimeline(Data, _layout);
                if (_layout is null || _layout.Rows.Count == 0) Put(Text(Strings.Empty, 13, "Muted"), 20, 95);
                if (focusTag is not null && focusState != FocusState.Unfocused)
                {
                    var target = _canvas.Children.OfType<Button>().FirstOrDefault(b => Equals(b.Tag, focusTag));
                    if (target is not null) target.Focus(focusState);
                    else _fit.Focus(focusState);
                }
            }
            if (selectionChanged) UpdateSelectionVisuals();
            if (shellChanged || selectionChanged) RenderDetails();
            UpdateTodayCaption();
        }
        finally { _rendering = false; }
        // Notify after drawing so a host can safely replace Data from the event handler.
        NotifySelection();
        diagnostics?.Invoke(this, new(System.Diagnostics.Stopwatch.GetElapsedTime(started), rebuilt, _canvas.Children.Count));
    }

    public void FitToView() { Zoom = 1; RefreshCore(); _scroller.ChangeView(0, 0, null); }
}
