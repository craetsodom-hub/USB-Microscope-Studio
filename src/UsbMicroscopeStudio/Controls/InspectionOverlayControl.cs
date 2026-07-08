using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using UsbMicroscopeStudio.Models.Inspection;
using UsbMicroscopeStudio.Services;

namespace UsbMicroscopeStudio.Controls;

public sealed class InspectionOverlayControl : FrameworkElement
{
    public static readonly DependencyProperty AnnotationsProperty = DependencyProperty.Register(
        nameof(Annotations),
        typeof(ObservableCollection<InspectionAnnotation>),
        typeof(InspectionOverlayControl),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnAnnotationsChanged));

    public static readonly DependencyProperty CurrentToolProperty = DependencyProperty.Register(
        nameof(CurrentTool),
        typeof(InspectionTool),
        typeof(InspectionOverlayControl),
        new FrameworkPropertyMetadata(InspectionTool.Select));

    public static readonly DependencyProperty StrokeColorProperty = DependencyProperty.Register(
        nameof(StrokeColor),
        typeof(string),
        typeof(InspectionOverlayControl),
        new FrameworkPropertyMetadata("#2F6FDB"));

    public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register(
        nameof(StrokeThickness),
        typeof(double),
        typeof(InspectionOverlayControl),
        new FrameworkPropertyMetadata(2d));

    public static readonly DependencyProperty ShowCrosshairProperty = DependencyProperty.Register(
        nameof(ShowCrosshair),
        typeof(bool),
        typeof(InspectionOverlayControl),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ShowGridProperty = DependencyProperty.Register(
        nameof(ShowGrid),
        typeof(bool),
        typeof(InspectionOverlayControl),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty GridSpacingPixelsProperty = DependencyProperty.Register(
        nameof(GridSpacingPixels),
        typeof(double),
        typeof(InspectionOverlayControl),
        new FrameworkPropertyMetadata(64d, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ShowRulersProperty = DependencyProperty.Register(
        nameof(ShowRulers),
        typeof(bool),
        typeof(InspectionOverlayControl),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty CaptureHistoryCommandProperty = DependencyProperty.Register(
        nameof(CaptureHistoryCommand),
        typeof(ICommand),
        typeof(InspectionOverlayControl));

    private InspectionAnnotation? _activeAnnotation;
    private InspectionPoint? _dragStart;
    private Guid? _selectedAnnotationId;
    private bool _isMovingSelection;

    public InspectionOverlayControl()
    {
        Focusable = true;
        ClipToBounds = true;
        MouseDown += OnMouseDown;
        MouseMove += OnMouseMove;
        MouseUp += OnMouseUp;
        KeyDown += OnKeyDown;
    }

    public ObservableCollection<InspectionAnnotation>? Annotations
    {
        get => (ObservableCollection<InspectionAnnotation>?)GetValue(AnnotationsProperty);
        set => SetValue(AnnotationsProperty, value);
    }

    public InspectionTool CurrentTool
    {
        get => (InspectionTool)GetValue(CurrentToolProperty);
        set => SetValue(CurrentToolProperty, value);
    }

    public string StrokeColor
    {
        get => (string)GetValue(StrokeColorProperty);
        set => SetValue(StrokeColorProperty, value);
    }

    public double StrokeThickness
    {
        get => (double)GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    public bool ShowCrosshair
    {
        get => (bool)GetValue(ShowCrosshairProperty);
        set => SetValue(ShowCrosshairProperty, value);
    }

    public bool ShowGrid
    {
        get => (bool)GetValue(ShowGridProperty);
        set => SetValue(ShowGridProperty, value);
    }

    public double GridSpacingPixels
    {
        get => (double)GetValue(GridSpacingPixelsProperty);
        set => SetValue(GridSpacingPixelsProperty, value);
    }

    public bool ShowRulers
    {
        get => (bool)GetValue(ShowRulersProperty);
        set => SetValue(ShowRulersProperty, value);
    }

    public ICommand? CaptureHistoryCommand
    {
        get => (ICommand?)GetValue(CaptureHistoryCommandProperty);
        set => SetValue(CaptureHistoryCommandProperty, value);
    }

    private static void OnAnnotationsChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        var control = (InspectionOverlayControl)dependencyObject;
        if (e.OldValue is INotifyCollectionChanged oldCollection)
        {
            oldCollection.CollectionChanged -= control.AnnotationsOnCollectionChanged;
        }

        if (e.NewValue is INotifyCollectionChanged newCollection)
        {
            newCollection.CollectionChanged += control.AnnotationsOnCollectionChanged;
        }
    }

    private void AnnotationsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        DrawGrid(drawingContext);
        DrawRulers(drawingContext);
        DrawCrosshair(drawingContext);
        DrawAnnotations(drawingContext);
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        Focus();
        var point = ToInspectionPoint(e.GetPosition(this));
        _dragStart = point;

        if (CurrentTool == InspectionTool.Select)
        {
            _selectedAnnotationId = HitTest(point);
            if (_selectedAnnotationId is not null && e.ClickCount == 2)
            {
                EditSelectedTextAnnotation();
                return;
            }

            if (_selectedAnnotationId is not null)
            {
                CaptureHistoryCommand?.Execute(null);
                _isMovingSelection = true;
                CaptureMouse();
            }

            return;
        }

        if (CurrentTool == InspectionTool.Text)
        {
            var text = PromptForText(null);
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            CaptureHistoryCommand?.Execute(null);
            Annotations?.Add(new InspectionAnnotation
            {
                Tool = CurrentTool,
                StrokeColor = StrokeColor,
                StrokeThickness = Math.Max(1, StrokeThickness),
                Text = text,
                Points = [point]
            });
            InvalidateVisual();
            return;
        }

        if (CurrentTool == InspectionTool.Angle)
        {
            HandleAngleClick(point);
            return;
        }

        CaptureHistoryCommand?.Execute(null);
        _activeAnnotation = new InspectionAnnotation
        {
            Tool = CurrentTool,
            StrokeColor = StrokeColor,
            StrokeThickness = Math.Max(1, StrokeThickness),
            IsMeasurement = CurrentTool is InspectionTool.ReferenceLine or InspectionTool.Distance or InspectionTool.Angle,
            Points = [point, point]
        };

        Annotations?.Add(_activeAnnotation);
        CaptureMouse();
        InvalidateVisual();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        var point = ToInspectionPoint(e.GetPosition(this));

        if (e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        if (CurrentTool == InspectionTool.Select && _isMovingSelection && _selectedAnnotationId is not null && _dragStart is not null)
        {
            var annotation = Annotations?.FirstOrDefault(item => item.Id == _selectedAnnotationId);
            if (annotation is null)
            {
                return;
            }

            var dx = point.X - _dragStart.Value.X;
            var dy = point.Y - _dragStart.Value.Y;
            ReplaceAnnotation(annotation with
            {
                Points = annotation.Points.Select(p => InspectionGeometry.Translate(p, dx, dy)).ToList()
            });
            _dragStart = point;
            InvalidateVisual();
            return;
        }

        if (_activeAnnotation is null)
        {
            return;
        }

        var points = _activeAnnotation.Tool == InspectionTool.Freehand
            ? [.. _activeAnnotation.Points, point]
            : new List<InspectionPoint> { _activeAnnotation.Points[0], point };

        _activeAnnotation = _activeAnnotation with { Points = points };
        ReplaceAnnotation(_activeAnnotation);
        InvalidateVisual();
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_activeAnnotation?.Tool != InspectionTool.Angle)
        {
            _activeAnnotation = null;
        }

        _dragStart = null;
        _isMovingSelection = false;
        ReleaseMouseCapture();
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Delete || _selectedAnnotationId is null || Annotations is null)
        {
            return;
        }

        var annotation = Annotations.FirstOrDefault(item => item.Id == _selectedAnnotationId);
        if (annotation is null)
        {
            return;
        }

        CaptureHistoryCommand?.Execute(null);
        Annotations.Remove(annotation);
        _selectedAnnotationId = null;
        InvalidateVisual();
    }

    private void DrawGrid(DrawingContext drawingContext)
    {
        if (!ShowGrid)
        {
            return;
        }

        var spacing = Math.Max(8, GridSpacingPixels);
        var pen = new Pen(new SolidColorBrush(Color.FromArgb(90, 180, 190, 198)), 1);
        pen.Freeze();

        for (var x = 0d; x <= ActualWidth; x += spacing)
        {
            drawingContext.DrawLine(pen, new Point(x, 0), new Point(x, ActualHeight));
        }

        for (var y = 0d; y <= ActualHeight; y += spacing)
        {
            drawingContext.DrawLine(pen, new Point(0, y), new Point(ActualWidth, y));
        }
    }

    private void DrawRulers(DrawingContext drawingContext)
    {
        if (!ShowRulers)
        {
            return;
        }

        var pen = new Pen(Brushes.White, 1);
        var tickPen = new Pen(Brushes.White, 1);
        drawingContext.DrawLine(pen, new Point(0, 0), new Point(ActualWidth, 0));
        drawingContext.DrawLine(pen, new Point(0, 0), new Point(0, ActualHeight));

        for (var x = 0d; x <= ActualWidth; x += 50)
        {
            drawingContext.DrawLine(tickPen, new Point(x, 0), new Point(x, x % 100 == 0 ? 14 : 8));
        }

        for (var y = 0d; y <= ActualHeight; y += 50)
        {
            drawingContext.DrawLine(tickPen, new Point(0, y), new Point(y % 100 == 0 ? 14 : 8, y));
        }
    }

    private void DrawCrosshair(DrawingContext drawingContext)
    {
        if (!ShowCrosshair)
        {
            return;
        }

        var pen = new Pen(new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)), 1);
        drawingContext.DrawLine(pen, new Point(ActualWidth / 2, 0), new Point(ActualWidth / 2, ActualHeight));
        drawingContext.DrawLine(pen, new Point(0, ActualHeight / 2), new Point(ActualWidth, ActualHeight / 2));
    }

    private void DrawAnnotations(DrawingContext drawingContext)
    {
        if (Annotations is null)
        {
            return;
        }

        foreach (var annotation in Annotations)
        {
            DrawAnnotation(drawingContext, annotation);
        }
    }

    private void DrawAnnotation(DrawingContext drawingContext, InspectionAnnotation annotation)
    {
        if (annotation.Points.Count == 0)
        {
            return;
        }

        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(annotation.StrokeColor));
        var pen = new Pen(brush, annotation.StrokeThickness);
        var points = annotation.Points.Select(point => InspectionGeometry.ToViewport(point, ActualWidth, ActualHeight)).ToList();

        switch (annotation.Tool)
        {
            case InspectionTool.Rectangle:
                drawingContext.DrawRectangle(null, pen, RectFrom(points[0], points[^1]));
                break;
            case InspectionTool.Circle:
                var rect = RectFrom(points[0], points[^1]);
                drawingContext.DrawEllipse(null, pen, rect.Location + new Vector(rect.Width / 2, rect.Height / 2), rect.Width / 2, rect.Height / 2);
                break;
            case InspectionTool.Freehand:
                for (var i = 1; i < points.Count; i++)
                {
                    drawingContext.DrawLine(pen, points[i - 1], points[i]);
                }
                break;
            case InspectionTool.Text:
                drawingContext.DrawText(
                    new FormattedText(annotation.Text ?? string.Empty, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 18, brush, VisualTreeHelper.GetDpi(this).PixelsPerDip),
                    points[0]);
                break;
            case InspectionTool.Angle:
                DrawAngle(drawingContext, pen, brush, points);
                break;
            default:
                if (points.Count > 1)
                {
                    drawingContext.DrawLine(pen, points[0], points[^1]);
                    if (annotation.Tool == InspectionTool.Arrow)
                    {
                        DrawArrowHead(drawingContext, pen, points[0], points[^1]);
                    }
                }

                break;
        }

        if (_selectedAnnotationId == annotation.Id)
        {
            var bounds = BoundsFor(annotation);
            drawingContext.DrawRectangle(null, new Pen(Brushes.White, 1), bounds);
        }
    }

    private void HandleAngleClick(InspectionPoint point)
    {
        if (_activeAnnotation is null || _activeAnnotation.Tool != InspectionTool.Angle || _activeAnnotation.Points.Count >= 3)
        {
            CaptureHistoryCommand?.Execute(null);
            _activeAnnotation = new InspectionAnnotation
            {
                Tool = InspectionTool.Angle,
                StrokeColor = StrokeColor,
                StrokeThickness = Math.Max(1, StrokeThickness),
                IsMeasurement = true,
                Points = [point]
            };
            Annotations?.Add(_activeAnnotation);
            InvalidateVisual();
            return;
        }

        _activeAnnotation = _activeAnnotation with { Points = [.. _activeAnnotation.Points, point] };
        ReplaceAnnotation(_activeAnnotation);
        if (_activeAnnotation.Points.Count >= 3)
        {
            _activeAnnotation = null;
        }

        InvalidateVisual();
    }

    private void DrawAngle(DrawingContext drawingContext, Pen pen, Brush brush, IReadOnlyList<Point> points)
    {
        if (points.Count == 1)
        {
            drawingContext.DrawEllipse(brush, null, points[0], 3, 3);
            return;
        }

        if (points.Count == 2)
        {
            drawingContext.DrawLine(pen, points[0], points[1]);
            return;
        }

        var first = points[0];
        var vertex = points[1];
        var second = points[2];
        drawingContext.DrawLine(pen, vertex, first);
        drawingContext.DrawLine(pen, vertex, second);
        DrawAngleArc(drawingContext, pen, brush, first, vertex, second);
    }

    private void DrawAngleArc(DrawingContext drawingContext, Pen pen, Brush brush, Point first, Point vertex, Point second)
    {
        var startAngle = Math.Atan2(first.Y - vertex.Y, first.X - vertex.X);
        var endAngle = Math.Atan2(second.Y - vertex.Y, second.X - vertex.X);
        var sweep = NormalizeSweep(endAngle - startAngle);
        var radius = Math.Min(42, Math.Min((first - vertex).Length, (second - vertex).Length) * 0.35);
        radius = Math.Max(16, radius);

        var start = new Point(vertex.X + Math.Cos(startAngle) * radius, vertex.Y + Math.Sin(startAngle) * radius);
        var end = new Point(vertex.X + Math.Cos(startAngle + sweep) * radius, vertex.Y + Math.Sin(startAngle + sweep) * radius);
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(start, false, false);
            context.ArcTo(end, new Size(radius, radius), 0, sweep > Math.PI, SweepDirection.Clockwise, true, false);
        }

        geometry.Freeze();
        drawingContext.DrawGeometry(null, pen, geometry);

        var labelAngle = startAngle + (sweep / 2);
        var labelPoint = new Point(vertex.X + Math.Cos(labelAngle) * (radius + 14), vertex.Y + Math.Sin(labelAngle) * (radius + 14));
        var angleDegrees = Math.Abs(sweep * 180d / Math.PI);
        drawingContext.DrawText(
            new FormattedText($"{angleDegrees:0.#} deg", System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI Semibold"), 14, brush, VisualTreeHelper.GetDpi(this).PixelsPerDip),
            labelPoint);
    }

    private void EditSelectedTextAnnotation()
    {
        if (Annotations is null || _selectedAnnotationId is null)
        {
            return;
        }

        var annotation = Annotations.FirstOrDefault(item => item.Id == _selectedAnnotationId && item.Tool == InspectionTool.Text);
        if (annotation is null)
        {
            return;
        }

        var text = PromptForText(annotation.Text);
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        CaptureHistoryCommand?.Execute(null);
        ReplaceAnnotation(annotation with { Text = text });
        InvalidateVisual();
    }

    private void DrawArrowHead(DrawingContext drawingContext, Pen pen, Point start, Point end)
    {
        var angle = Math.Atan2(end.Y - start.Y, end.X - start.X);
        var length = 14;
        var left = new Point(end.X - length * Math.Cos(angle - Math.PI / 7), end.Y - length * Math.Sin(angle - Math.PI / 7));
        var right = new Point(end.X - length * Math.Cos(angle + Math.PI / 7), end.Y - length * Math.Sin(angle + Math.PI / 7));
        drawingContext.DrawLine(pen, end, left);
        drawingContext.DrawLine(pen, end, right);
    }

    private Guid? HitTest(InspectionPoint point)
    {
        var viewportPoint = InspectionGeometry.ToViewport(point, ActualWidth, ActualHeight);
        return Annotations?
            .LastOrDefault(annotation => BoundsFor(annotation).Contains(viewportPoint))
            ?.Id;
    }

    private void ReplaceAnnotation(InspectionAnnotation annotation)
    {
        if (Annotations is null)
        {
            return;
        }

        var index = Annotations.ToList().FindIndex(item => item.Id == annotation.Id);
        if (index >= 0)
        {
            Annotations[index] = annotation;
        }
    }

    private Rect BoundsFor(InspectionAnnotation annotation)
    {
        if (annotation.Points.Count == 0)
        {
            return Rect.Empty;
        }

        var points = annotation.Points.Select(point => InspectionGeometry.ToViewport(point, ActualWidth, ActualHeight)).ToList();
        var minX = points.Min(point => point.X);
        var minY = points.Min(point => point.Y);
        var maxX = points.Max(point => point.X);
        var maxY = points.Max(point => point.Y);
        return new Rect(new Point(minX - 8, minY - 8), new Point(maxX + 8, maxY + 8));
    }

    private static Rect RectFrom(Point a, Point b) => new(a, b);

    private InspectionPoint ToInspectionPoint(Point point) => InspectionGeometry.FromViewport(point, ActualWidth, ActualHeight);

    private string? PromptForText(string? initialValue)
    {
        var textBox = new TextBox
        {
            Text = initialValue ?? string.Empty,
            MinWidth = 280,
            Margin = new Thickness(0, 0, 0, 12),
            AcceptsReturn = true,
            MinLines = 2,
            MaxLines = 4
        };
        textBox.SelectAll();

        var okButton = new Button { Content = "OK", Width = 76, IsDefault = true, Margin = new Thickness(0, 0, 8, 0) };
        var cancelButton = new Button { Content = "Cancel", Width = 76, IsCancel = true };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        buttons.Children.Add(okButton);
        buttons.Children.Add(cancelButton);

        var panel = new StackPanel { Margin = new Thickness(16) };
        panel.Children.Add(new TextBlock { Text = "Text annotation", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 8) });
        panel.Children.Add(textBox);
        panel.Children.Add(buttons);

        var window = new Window
        {
            Title = "Text annotation",
            Content = panel,
            Owner = Window.GetWindow(this),
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
            SizeToContent = SizeToContent.WidthAndHeight
        };
        okButton.Click += (_, _) => window.DialogResult = true;
        var result = window.ShowDialog();
        return result == true ? textBox.Text.Trim() : null;
    }

    private static double NormalizeSweep(double sweep)
    {
        while (sweep < 0)
        {
            sweep += Math.PI * 2;
        }

        while (sweep > Math.PI * 2)
        {
            sweep -= Math.PI * 2;
        }

        return sweep > Math.PI ? Math.PI * 2 - sweep : sweep;
    }
}
