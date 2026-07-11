using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using UsbMicroscopeStudio.Models.Inspection;

namespace UsbMicroscopeStudio.Services;

public sealed class InspectionFrameRenderer
{
    public BitmapSource RenderAnnotatedFrame(
        BitmapSource cleanFrame,
        IReadOnlyList<InspectionAnnotation> annotations,
        OverlayOptions overlayOptions)
    {
        var width = Math.Max(1, cleanFrame.PixelWidth);
        var height = Math.Max(1, cleanFrame.PixelHeight);
        var visual = new DrawingVisual();
        using (var drawingContext = visual.RenderOpen())
        {
            drawingContext.DrawImage(cleanFrame, new Rect(0, 0, width, height));
            DrawGrid(drawingContext, overlayOptions, width, height);
            DrawRulers(drawingContext, overlayOptions, width, height);
            DrawCrosshair(drawingContext, overlayOptions, width, height);
            foreach (var annotation in annotations)
            {
                DrawAnnotation(drawingContext, annotation, width, height);
            }
        }

        var bitmap = new RenderTargetBitmap(width, height, cleanFrame.DpiX, cleanFrame.DpiY, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }

    private static void DrawGrid(DrawingContext drawingContext, OverlayOptions options, int width, int height)
    {
        if (!options.ShowGrid)
        {
            return;
        }

        var spacing = Math.Max(8, options.GridSpacingPixels);
        var pen = new Pen(new SolidColorBrush(Color.FromArgb(90, 180, 190, 198)), 1);
        for (var x = 0d; x <= width; x += spacing)
        {
            drawingContext.DrawLine(pen, new Point(x, 0), new Point(x, height));
        }

        for (var y = 0d; y <= height; y += spacing)
        {
            drawingContext.DrawLine(pen, new Point(0, y), new Point(width, y));
        }
    }

    private static void DrawRulers(DrawingContext drawingContext, OverlayOptions options, int width, int height)
    {
        if (!options.ShowRulers)
        {
            return;
        }

        var pen = new Pen(Brushes.White, 1);
        drawingContext.DrawLine(pen, new Point(0, 0), new Point(width, 0));
        drawingContext.DrawLine(pen, new Point(0, 0), new Point(0, height));
        for (var x = 0d; x <= width; x += 50)
        {
            drawingContext.DrawLine(pen, new Point(x, 0), new Point(x, x % 100 == 0 ? 14 : 8));
        }

        for (var y = 0d; y <= height; y += 50)
        {
            drawingContext.DrawLine(pen, new Point(0, y), new Point(y % 100 == 0 ? 14 : 8, y));
        }
    }

    private static void DrawCrosshair(DrawingContext drawingContext, OverlayOptions options, int width, int height)
    {
        if (!options.ShowCrosshair)
        {
            return;
        }

        var pen = new Pen(new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)), 1);
        drawingContext.DrawLine(pen, new Point(width / 2d, 0), new Point(width / 2d, height));
        drawingContext.DrawLine(pen, new Point(0, height / 2d), new Point(width, height / 2d));
    }

    private static void DrawAnnotation(DrawingContext drawingContext, InspectionAnnotation annotation, int width, int height)
    {
        if (annotation.Points.Count == 0)
        {
            return;
        }

        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(annotation.StrokeColor));
        var pen = new Pen(brush, annotation.StrokeThickness);
        var points = annotation.Points.Select(point => InspectionGeometry.ToViewport(point, width, height)).ToList();
        var bounds = new Rect(0, 0, width, height);

        switch (annotation.Tool)
        {
            case InspectionTool.Rectangle:
                drawingContext.DrawRectangle(null, pen, new Rect(points[0], points[^1]));
                break;
            case InspectionTool.Circle:
                var rect = new Rect(points[0], points[^1]);
                drawingContext.DrawEllipse(null, pen, rect.Location + new Vector(rect.Width / 2, rect.Height / 2), rect.Width / 2, rect.Height / 2);
                break;
            case InspectionTool.Freehand:
                for (var i = 1; i < points.Count; i++)
                {
                    drawingContext.DrawLine(pen, points[i - 1], points[i]);
                }

                break;
            case InspectionTool.Text:
                DrawLabel(drawingContext, annotation.Text ?? string.Empty, points[0], brush, 18, FontWeights.SemiBold, center: false, bounds);
                break;
            case InspectionTool.Arrow:
                DrawLineOrArrow(drawingContext, pen, points, true);
                break;
            case InspectionTool.Angle:
                DrawAngle(drawingContext, pen, brush, points, bounds);
                break;
            default:
                DrawLineOrArrow(drawingContext, pen, points, false);
                break;
        }
    }

    private static void DrawLineOrArrow(DrawingContext drawingContext, Pen pen, IReadOnlyList<Point> points, bool arrow)
    {
        if (points.Count < 2)
        {
            return;
        }

        drawingContext.DrawLine(pen, points[0], points[^1]);
        if (!arrow)
        {
            return;
        }

        var angle = Math.Atan2(points[^1].Y - points[0].Y, points[^1].X - points[0].X);
        const double length = 14;
        var left = new Point(points[^1].X - length * Math.Cos(angle - Math.PI / 7), points[^1].Y - length * Math.Sin(angle - Math.PI / 7));
        var right = new Point(points[^1].X - length * Math.Cos(angle + Math.PI / 7), points[^1].Y - length * Math.Sin(angle + Math.PI / 7));
        drawingContext.DrawLine(pen, points[^1], left);
        drawingContext.DrawLine(pen, points[^1], right);
    }

    private static void DrawAngle(DrawingContext drawingContext, Pen pen, Brush brush, IReadOnlyList<Point> points, Rect bounds)
    {
        if (points.Count < 3)
        {
            DrawLineOrArrow(drawingContext, pen, points, false);
            return;
        }

        var first = points[0];
        var vertex = points[1];
        var second = points[2];
        if (!InspectionGeometry.HasValidThreePointAngle(
                InspectionGeometry.FromViewport(first, bounds.Width, bounds.Height),
                InspectionGeometry.FromViewport(vertex, bounds.Width, bounds.Height),
                InspectionGeometry.FromViewport(second, bounds.Width, bounds.Height),
                bounds.Width,
                bounds.Height))
        {
            return;
        }

        drawingContext.DrawLine(pen, vertex, first);
        drawingContext.DrawLine(pen, vertex, second);

        var startAngle = Math.Atan2(first.Y - vertex.Y, first.X - vertex.X);
        var endAngle = Math.Atan2(second.Y - vertex.Y, second.X - vertex.X);
        var sweep = NormalizeSweep(endAngle - startAngle);
        var radius = Math.Max(16, Math.Min(42, Math.Min((first - vertex).Length, (second - vertex).Length) * 0.35));
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
        var labelPoint = new Point(vertex.X + Math.Cos(labelAngle) * (radius + 24), vertex.Y + Math.Sin(labelAngle) * (radius + 24));
        DrawLabel(drawingContext, $"{Math.Abs(sweep * 180d / Math.PI):0.#}°", labelPoint, brush, 14, FontWeights.SemiBold, center: true, bounds);
    }

    private static void DrawLabel(DrawingContext drawingContext, string text, Point origin, Brush foreground, double fontSize, FontWeight weight, bool center, Rect bounds)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var formatted = new FormattedText(
            text,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, weight, FontStretches.Normal),
            fontSize,
            foreground,
            1);

        const double horizontalPadding = 6;
        const double verticalPadding = 3;
        var x = center ? origin.X - (formatted.Width / 2) : origin.X;
        var y = center ? origin.Y - (formatted.Height / 2) : origin.Y;
        x = Math.Clamp(x, bounds.Left + 2, Math.Max(bounds.Left + 2, bounds.Right - formatted.Width - (horizontalPadding * 2) - 2));
        y = Math.Clamp(y, bounds.Top + 2, Math.Max(bounds.Top + 2, bounds.Bottom - formatted.Height - (verticalPadding * 2) - 2));

        var rect = new Rect(
            new Point(x - horizontalPadding, y - verticalPadding),
            new Size(formatted.Width + (horizontalPadding * 2), formatted.Height + (verticalPadding * 2)));
        var background = new SolidColorBrush(Color.FromArgb(218, 7, 11, 16));
        var border = new Pen(new SolidColorBrush(Color.FromArgb(170, 116, 145, 168)), 1);
        var halo = new SolidColorBrush(Color.FromArgb(80, 0, 0, 0));

        drawingContext.DrawRoundedRectangle(halo, null, new Rect(rect.X + 1, rect.Y + 1, rect.Width, rect.Height), 4, 4);
        drawingContext.DrawRoundedRectangle(background, border, rect, 4, 4);
        drawingContext.DrawText(formatted, new Point(x, y));
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
