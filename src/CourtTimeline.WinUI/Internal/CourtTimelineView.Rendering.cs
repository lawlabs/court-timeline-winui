using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;

namespace CourtTimeline;

public sealed partial class CourtTimelineView
{
    private Brush Brush(string name) => TimelineTheme.Get(this, name, _highContrast);
    private Brush Tone(CourtStage stage, bool faint = false) => TimelineTheme.Tone(this, stage.Tone, _highContrast, faint);
    private string Day(DateOnly date) => date.ToString("d MMM", Culture).TrimEnd('.');
    private TextBlock Text(string value, double size, string brush = "Text", bool bold = false) => new()
    {
        Text = value, FontSize = size, Foreground = Brush(brush), Language = Culture.IetfLanguageTag,
        FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal, TextTrimming = TextTrimming.CharacterEllipsis
    };
    private void Put(FrameworkElement element, double x, double y)
    {
        Canvas.SetLeft(element, x); Canvas.SetTop(element, y); _canvas.Children.Add(element);
    }
    private void Line(double x1, double y1, double x2, double y2, Brush brush, bool dashed = false)
    {
        var line = new Line { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, Stroke = brush, StrokeThickness = 1, IsHitTestVisible = false };
        if (dashed) line.StrokeDashArray = [4, 4];
        _canvas.Children.Add(line);
    }

    private void RenderShell()
    {
        _root.Background = Brush("Surface");
        _header.Visibility = ShowCaseHeader && Data is not null ? Visibility.Visible : Visibility.Collapsed;
        _case.Children.Clear(); _amount.Children.Clear();
        if (Data is { } data)
        {
            _case.Children.Add(Text((data.Number.Length > 0 ? string.Format(Culture, Strings.CaseNumber, data.Number) : "")
                + (data.Status.Length > 0 ? " · " + data.Status : ""), 12, "Brand"));
            _case.Children.Add(Text(data.Title, 22, bold: true));
            var subject = Text(data.Subject, 12, "Muted"); subject.TextWrapping = TextWrapping.Wrap;
            _case.Children.Add(subject);
            if (data.Amount.Length > 0) { _amount.Children.Add(Text(Strings.Amount, 11, "Muted")); _amount.Children.Add(Text(data.Amount, 22, bold: true)); }
        }
        _summary.Children.Clear();
        _summary.Children.Add(Text(Strings.Progress, 14, bold: true));
        _summary.Children.Add(Text(string.Format(Culture, Strings.Counts, _layout?.Rows.Count ?? 0,
            _layout?.Rows.Sum(r => r.Events.Count) ?? 0), 11, "Muted"));
        bool narrow = ActualWidth < 780;
        Grid.SetColumn(_actions, narrow ? 0 : 1); Grid.SetRow(_actions, narrow ? 1 : 0);
        Grid.SetColumnSpan(_summary, narrow ? 2 : 1); Grid.SetColumnSpan(_actions, narrow ? 2 : 1);
        _actions.HorizontalAlignment = narrow ? HorizontalAlignment.Left : HorizontalAlignment.Right;
        _plan.Content = Strings.ShowPlan; _plan.IsChecked = ShowPlan;
        _fit.Content = Zoom == 1 ? Strings.Fit : $"{Zoom.ToString("0.#", Culture)}× · {Strings.Fit}";
        _zoomOut.IsEnabled = Zoom > 1; _zoomIn.IsEnabled = Zoom < 4;
        AutomationProperties.SetName(_plan, Strings.ShowPlan);
        AutomationProperties.SetName(_fit, Strings.Fit);
        AutomationProperties.SetName(_zoomIn, Strings.ZoomIn); AutomationProperties.SetName(_zoomOut, Strings.ZoomOut);
        _frame.BorderBrush = Brush("Line"); _legend.Text = Strings.Legend; _legend.Foreground = Brush("Muted");
        _details.Background = Brush("Shell"); _details.BorderBrush = Brush("Line");
        _details.Visibility = ShowDetails ? Visibility.Visible : Visibility.Collapsed;
    }

    private void RenderTimeline(CourtTimelineData data, TimelineLayout.Result layout)
    {
        double X(DateOnly date) => layout.X(date, data.Start);
        Put(new Border { Width = layout.Width, Height = 44, Background = Brush("Shell") }, 0, 0);
        var axis = Text(Strings.Axis, 10, "Muted", true); axis.Width = TimelineLayout.LabelWidth - 20; Put(axis, 12, 15);
        var month = new DateOnly(data.Start.Year, data.Start.Month, 1);
        while (month < data.End)
        {
            var next = month.Year == 9999 && month.Month == 12 ? DateOnly.MaxValue : month.AddMonths(1);
            double left = X(month < data.Start ? data.Start : month);
            double right = X(next > data.End ? data.End : next);
            if (right - left >= 2) Line(left, 0, left, layout.Height, Brush("Line"));
            if (right - left >= 38)
            {
                var label = Text(month.ToString("MMM yyyy", Culture).TrimEnd('.'), 10, "Muted", true);
                label.Width = right - left; label.TextAlignment = TextAlignment.Center; Put(label, left, 15);
            }
            if (next <= month || next >= data.End) break;
            month = next;
        }
        if (layout.DayWidth >= 10)
        {
            for (int day = data.Start.DayNumber; day < data.End.DayNumber; day++)
            {
                var date = DateOnly.FromDayNumber(day);
                Line(X(date), 44, X(date), layout.Height, Brush("SubtleLine"));
                if (layout.DayWidth < 20) continue;
                var number = Text(date.Day.ToString(Culture), 9, "Muted");
                number.Width = layout.DayWidth; number.TextAlignment = TextAlignment.Center; Put(number, X(date), 46);
            }
        }
        var today = data.Today ?? DateOnly.FromDateTime(DateTime.Today);
        if (today >= data.Start && today < data.End)
        {
            double point = X(today) + layout.DayWidth / 2;
            Line(point, 44, point, layout.Height, Brush("Now"), true);
            var label = new Border { Background = Brush("Surface"), Padding = new Thickness(5, 2, 5, 2),
                Child = Text($"{Day(today)} · {Strings.Today}", 9, "Now", true), MaxWidth = 170 };
            Put(label, Math.Clamp(point - 72, TimelineLayout.LabelWidth, layout.Width - 175), 45);
        }
        foreach (var row in layout.Rows) RenderRow(row);
    }

    private void RenderRow(TimelineLayout.Row row)
    {
        var stage = row.Stage;
        var color = Tone(stage);
        double y = row.Top;
        Line(0, y + row.Height - 16, _canvas.Width, y + row.Height - 16, Brush("Line"));
        var title = Text(stage.Title, 12, bold: true); title.Width = TimelineLayout.LabelWidth - 28; Put(title, 16, y + 2);
        var court = Text(stage.Court, 10, "Muted"); court.Width = TimelineLayout.LabelWidth - 28; Put(court, 16, y + 25);
        var status = Text(stage.Status, 10, stage.State == CourtStageState.Active ? "Brand" : "Muted");
        status.Width = TimelineLayout.LabelWidth - 28; Put(status, 16, y + 46);
        if (row.Width > 0)
        {
            var band = new Grid { Width = row.Width, Height = 52, Clip = new RectangleGeometry { Rect = new Rect(0, 0, row.Width, 52) } };
            band.Children.Add(new Border { Width = row.FactWidth, HorizontalAlignment = HorizontalAlignment.Left, Background = Tone(stage, true), CornerRadius = new CornerRadius(5) });
            var hatch = new Canvas { IsHitTestVisible = false, Clip = new RectangleGeometry { Rect = new Rect(row.FactWidth, 0, row.Width - row.FactWidth, 52) } };
            for (double x = row.FactWidth - 52; x < row.Width; x += 9)
                hatch.Children.Add(new Line { X1 = x, Y1 = 52, X2 = x + 52, Y2 = 0, Stroke = color, Opacity = _highContrast ? 1 : .16, StrokeThickness = 1 });
            band.Children.Add(hatch);
            var selection = new CourtTimelineSelection(CourtTimelineItemKind.Stage, stage.Id);
            var outline = new Rectangle { Stroke = color, StrokeThickness = SelectedItem == selection ? 2 : 1, RadiusX = 5, RadiusY = 5, IsHitTestVisible = false };
            if (stage.State == CourtStageState.Potential) outline.StrokeDashArray = [5, 4];
            band.Children.Add(outline);
            var copy = new StackPanel { Spacing = 3, Margin = new Thickness(12, 7, 10, 5) };
            var bandTitle = Text(stage.Title, 12, bold: true); bandTitle.Foreground = color; copy.Children.Add(bandTitle);
            copy.Children.Add(Text($"{Day(stage.Start)} — {Day(row.VisualEnd)}" + (stage.State == CourtStageState.Potential ? " · " + Strings.Potential : ""), 10, "Muted"));
            band.Children.Add(copy);
            var button = ItemButton(band, selection, $"{stage.Title}, {stage.Status}, {stage.Start.ToString("D", Culture)} — {row.VisualEnd.ToString("D", Culture)}", stage.Note);
            button.Padding = new Thickness(0); button.MinWidth = 0; button.MinHeight = 0; button.BorderThickness = new Thickness(0);
            button.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
            Put(button, row.Left, y);
        }
        foreach (var placement in row.Events)
        {
            var item = placement.Event;
            double labelTop = y + 69 + placement.Lane * 44;
            Line(placement.Point, y + 53, placement.Point, labelTop + 6, color, item.Planned);
            Put(new Ellipse { Width = 8, Height = 8, Fill = item.Planned ? Brush("Surface") : color, Stroke = color, StrokeThickness = 1.5 }, placement.Point - 4, y + 60);
            var selection = new CourtTimelineSelection(CourtTimelineItemKind.Event, item.Id);
            var copy = new StackPanel { Spacing = 2 };
            copy.Children.Add(Text(Day(item.Date) + (item.Time.Length > 0 ? " · " + item.Time : ""), 9, "Muted"));
            copy.Children.Add(Text(item.Title, 10, bold: selection == SelectedItem));
            var button = ItemButton(copy, selection, $"{item.Title}, {item.Date.ToString("D", Culture)} {item.Time}, {(item.Planned ? Strings.Planned : Strings.Occurred)}", item.Note);
            button.Width = TimelineLayout.EventWidth; button.Height = 40;
            button.Padding = new Thickness(7, 3, 7, 3); button.CornerRadius = new CornerRadius(4);
            button.Background = Brush(selection == SelectedItem ? "Today" : "Surface");
            button.BorderBrush = selection == SelectedItem ? color : Brush("Line");
            button.BorderThickness = new Thickness(selection == SelectedItem ? 2 : 1);
            Canvas.SetZIndex(button, 2); Put(button, placement.Left, labelTop);
        }
    }

    private Button ItemButton(UIElement content, CourtTimelineSelection selection, string name, string note)
    {
        var button = new Button { Content = content, Tag = selection, HorizontalContentAlignment = HorizontalAlignment.Stretch };
        AutomationProperties.SetName(button, name);
        AutomationProperties.SetAutomationId(button, $"CourtTimeline.{selection.Kind}.{selection.Id}");
        ToolTipService.SetToolTip(button, note.Length > 0 ? name + "\n" + note : name);
        button.Click += (_, _) => SelectedItem = selection;
        return button;
    }

    private void RenderDetails()
    {
        var copy = new StackPanel { Spacing = 6 };
        var selection = SelectedItem;
        var item = selection?.Kind == CourtTimelineItemKind.Event ? Data?.Events.FirstOrDefault(e => e.Id == selection.Id) : null;
        var stage = selection?.Kind == CourtTimelineItemKind.Stage ? Data?.Stages.FirstOrDefault(s => s.Id == selection.Id) : null;
        void Add(string value, double size, string brush = "Text", bool bold = false)
        {
            if (value.Length == 0) return;
            var text = Text(value, size, brush, bold); text.TextWrapping = TextWrapping.Wrap; copy.Children.Add(text);
        }
        if (item is not null)
        {
            Add(item.Kind + " · " + (item.Planned ? Strings.Planned : Strings.Occurred), 10, "Brand");
            Add(item.Title, 15, bold: true); Add(item.Date.ToString("D", Culture) + (item.Time.Length > 0 ? " · " + item.Time : ""), 12, "Muted");
            Add(item.Location, 12, bold: true); Add(item.Note, 12, "Muted");
        }
        else if (stage is not null)
        {
            Add(Strings.Stage + " · " + stage.Status, 10, "Brand"); Add(stage.Title, 15, bold: true);
            Add($"{stage.Start.ToString("d MMM yyyy", Culture)} — {stage.End.ToString("d MMM yyyy", Culture)}", 12, "Muted");
            Add(stage.Court, 12, bold: true); Add(stage.Note, 12, "Muted");
        }
        else Add(Strings.SelectItem, 12, "Muted");
        _details.Child = new ScrollViewer { Content = copy, MaxHeight = 190, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
    }
}
