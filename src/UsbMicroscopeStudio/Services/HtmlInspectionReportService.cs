using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using UsbMicroscopeStudio.Models.Inspection;
using UsbMicroscopeStudio.Models.Reports;
using UsbMicroscopeStudio.Models.Sessions;

namespace UsbMicroscopeStudio.Services;

public sealed class HtmlInspectionReportService
{
    private readonly Func<DateTimeOffset> _now;

    public HtmlInspectionReportService()
        : this(() => DateTimeOffset.Now)
    {
    }

    public HtmlInspectionReportService(Func<DateTimeOffset> now)
    {
        _now = now;
    }

    public HtmlInspectionReportResult Export(InspectionSessionDocument session)
    {
        if (string.IsNullOrWhiteSpace(session.SessionFolderPath))
        {
            throw new InvalidOperationException("Save the inspection session before exporting a report.");
        }

        var generatedAt = _now();
        var reportsFolder = Path.Combine(session.SessionFolderPath, "reports");
        var assetsFolder = Path.Combine(reportsFolder, "assets");
        Directory.CreateDirectory(reportsFolder);
        Directory.CreateDirectory(assetsFolder);

        var reportPath = Path.Combine(reportsFolder, $"report-{generatedAt:yyyyMMdd-HHmmss}.html");
        var copiedImages = new List<string>();
        var cleanImage = CopyReportImage(session.CleanFramePath, assetsFolder, "clean-frame", copiedImages);
        var annotatedImage = CopyReportImage(session.AnnotatedFramePath, assetsFolder, "annotated-frame", copiedImages);

        File.WriteAllText(reportPath, BuildHtml(session, generatedAt, cleanImage, annotatedImage), Encoding.UTF8);
        return new HtmlInspectionReportResult(reportPath, reportsFolder, copiedImages);
    }

    private static string BuildHtml(InspectionSessionDocument session, DateTimeOffset generatedAt, string? cleanImage, string? annotatedImage)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<!doctype html>");
        builder.AppendLine("<html lang=\"en\">");
        builder.AppendLine("<head>");
        builder.AppendLine("<meta charset=\"utf-8\">");
        builder.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        builder.AppendLine("<title>USB Microscope Studio Inspection Report</title>");
        builder.AppendLine("<style>");
        builder.AppendLine("body{font-family:Segoe UI,Arial,sans-serif;margin:0;background:#f4f6f8;color:#1f2328;line-height:1.45}");
        builder.AppendLine("main{max-width:1120px;margin:0 auto;padding:28px}");
        builder.AppendLine("header{border-bottom:3px solid #2f6f8f;padding-bottom:18px;margin-bottom:24px}");
        builder.AppendLine("h1{font-size:28px;margin:0 0 6px}h2{font-size:18px;margin:24px 0 10px;color:#243746}");
        builder.AppendLine(".muted{color:#5f6b76}.grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:10px 24px}");
        builder.AppendLine(".field{background:#fff;border:1px solid #d8dee4;padding:10px 12px}.label{font-size:12px;color:#5f6b76;text-transform:uppercase;letter-spacing:.04em}");
        builder.AppendLine(".value{font-size:15px;margin-top:3px;white-space:pre-wrap}.images{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:18px}");
        builder.AppendLine("figure{margin:0;background:#fff;border:1px solid #d8dee4;padding:12px}figcaption{font-weight:600;margin-bottom:8px}");
        builder.AppendLine("img{max-width:100%;height:auto;border:1px solid #d8dee4;background:#111}");
        builder.AppendLine("table{width:100%;border-collapse:collapse;background:#fff;border:1px solid #d8dee4}th,td{padding:9px 10px;border-bottom:1px solid #d8dee4;text-align:left;vertical-align:top}th{background:#eef3f6;font-size:12px;text-transform:uppercase;color:#374955}");
        builder.AppendLine(".missing{background:#fff;border:1px dashed #a0aab4;padding:18px;color:#5f6b76}.footer{margin-top:28px;font-size:12px;color:#5f6b76}");
        builder.AppendLine("@media(max-width:800px){.grid,.images{grid-template-columns:1fr}main{padding:18px}}");
        builder.AppendLine("</style>");
        builder.AppendLine("</head>");
        builder.AppendLine("<body><main>");
        builder.AppendLine("<header>");
        builder.AppendLine("<div class=\"muted\">USB Microscope Studio</div>");
        builder.AppendLine("<h1>Inspection Report</h1>");
        builder.AppendLine($"<div class=\"muted\">Generated {H(generatedAt.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture))}</div>");
        builder.AppendLine("</header>");

        builder.AppendLine("<section>");
        builder.AppendLine("<h2>Session</h2>");
        builder.AppendLine("<div class=\"grid\">");
        AddField(builder, "Project", session.SessionName);
        AddField(builder, "Customer", session.CustomerName);
        AddField(builder, "Device model", session.DeviceModel);
        AddField(builder, "Serial / asset tag", session.SerialAssetTag);
        AddField(builder, "Technician", session.TechnicianName);
        AddField(builder, "Job / order number", session.JobOrderNumber);
        AddField(builder, "Inspection date/time", session.InspectionDateTime.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture));
        AddField(builder, "Calibration", CalibrationSummary(session));
        builder.AppendLine("</div>");
        AddWideField(builder, "Notes", session.Notes);
        builder.AppendLine("</section>");

        builder.AppendLine("<section>");
        builder.AppendLine("<h2>Images</h2>");
        builder.AppendLine("<div class=\"images\">");
        AddImage(builder, "Clean frame", cleanImage, session.CleanFramePath);
        AddImage(builder, "Annotated frame", annotatedImage, session.AnnotatedFramePath);
        builder.AppendLine("</div>");
        builder.AppendLine("</section>");

        builder.AppendLine("<section>");
        builder.AppendLine("<h2>Annotation And Measurement Summary</h2>");
        AddSummaryTable(builder, session);
        builder.AppendLine("</section>");

        builder.AppendLine($"<div class=\"footer\">Report generated by USB Microscope Studio at {H(generatedAt.ToString("O", CultureInfo.InvariantCulture))}.</div>");
        builder.AppendLine("</main></body></html>");
        return builder.ToString();
    }

    private static void AddField(StringBuilder builder, string label, string? value)
    {
        builder.AppendLine("<div class=\"field\">");
        builder.AppendLine($"<div class=\"label\">{H(label)}</div>");
        builder.AppendLine($"<div class=\"value\">{H(Display(value))}</div>");
        builder.AppendLine("</div>");
    }

    private static void AddWideField(StringBuilder builder, string label, string? value)
    {
        builder.AppendLine("<div class=\"field\" style=\"margin-top:12px\">");
        builder.AppendLine($"<div class=\"label\">{H(label)}</div>");
        builder.AppendLine($"<div class=\"value\">{H(Display(value))}</div>");
        builder.AppendLine("</div>");
    }

    private static void AddImage(StringBuilder builder, string label, string? relativePath, string? originalPath)
    {
        builder.AppendLine("<figure>");
        builder.AppendLine($"<figcaption>{H(label)}</figcaption>");
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            builder.AppendLine($"<div class=\"missing\">No image available{(string.IsNullOrWhiteSpace(originalPath) ? string.Empty : $": {H(originalPath)}")}</div>");
        }
        else
        {
            builder.AppendLine($"<img src=\"{H(relativePath)}\" alt=\"{H(label)}\">");
        }

        builder.AppendLine("</figure>");
    }

    private static void AddSummaryTable(StringBuilder builder, InspectionSessionDocument session)
    {
        builder.AppendLine("<table>");
        builder.AppendLine("<thead><tr><th>#</th><th>Type</th><th>Details</th><th>Pixel length</th><th>Real length</th><th>Angle</th><th>Status</th></tr></thead>");
        builder.AppendLine("<tbody>");
        if (session.Annotations.Count == 0)
        {
            builder.AppendLine("<tr><td colspan=\"7\">No annotations or measurements recorded.</td></tr>");
        }
        else
        {
            var measurementIndex = 0;
            for (var index = 0; index < session.Annotations.Count; index++)
            {
                var annotation = session.Annotations[index];
                var measurement = IsMeasurementAnnotation(annotation) && measurementIndex < session.Measurements.Count
                    ? session.Measurements[measurementIndex++]
                    : null;
                builder.AppendLine("<tr>");
                builder.AppendLine($"<td>{index + 1}</td>");
                builder.AppendLine($"<td>{H(annotation.Tool.ToString())}</td>");
                builder.AppendLine($"<td>{H(AnnotationDetails(annotation))}</td>");
                builder.AppendLine($"<td>{H(FormatNumber(measurement?.PixelLength))}</td>");
                builder.AppendLine($"<td>{H(FormatRealLength(measurement))}</td>");
                builder.AppendLine($"<td>{H(FormatAngle(measurement?.AngleDegrees))}</td>");
                builder.AppendLine($"<td>{H(measurement is null ? "Annotation" : measurement.IsCalibrated ? "Calibrated" : "Uncalibrated")}</td>");
                builder.AppendLine("</tr>");
            }
        }

        builder.AppendLine("</tbody></table>");
    }

    private static string? CopyReportImage(string? sourcePath, string assetsFolder, string prefix, List<string> copiedImages)
    {
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
        {
            return null;
        }

        var extension = Path.GetExtension(sourcePath);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".png";
        }

        var destination = Path.Combine(assetsFolder, $"{prefix}{extension.ToLowerInvariant()}");
        for (var index = 1; File.Exists(destination); index++)
        {
            destination = Path.Combine(assetsFolder, $"{prefix}-{index:000}{extension.ToLowerInvariant()}");
        }

        File.Copy(sourcePath, destination);
        copiedImages.Add(destination);
        return ToRelativeUri(Path.GetDirectoryName(destination)!, Path.GetFileName(destination));
    }

    private static string ToRelativeUri(string folder, string fileName)
    {
        var relative = Path.Combine(Path.GetFileName(folder), fileName);
        return relative.Replace('\\', '/');
    }

    private static string CalibrationSummary(InspectionSessionDocument session)
    {
        var profileName = session.CalibrationProfile?.Name;
        return string.IsNullOrWhiteSpace(profileName)
            ? session.CalibrationStatus
            : $"{session.CalibrationStatus} ({profileName})";
    }

    private static bool IsMeasurementAnnotation(InspectionAnnotation annotation) =>
        annotation.IsMeasurement || annotation.Tool is InspectionTool.Angle or InspectionTool.Distance or InspectionTool.ReferenceLine;

    private static string AnnotationDetails(InspectionAnnotation annotation) =>
        annotation.Tool == InspectionTool.Text && !string.IsNullOrWhiteSpace(annotation.Text)
            ? annotation.Text
            : $"{annotation.Points.Count} point(s)";

    private static string FormatNumber(double? value) =>
        value.HasValue ? value.Value.ToString("0.###", CultureInfo.InvariantCulture) : "-";

    private static string FormatAngle(double? value) =>
        value.HasValue ? $"{value.Value:0.###}°" : "-";

    private static string FormatRealLength(MeasurementResult? measurement)
    {
        if (measurement?.RealLength is null)
        {
            return "-";
        }

        var suffix = measurement.Units == InspectionUnits.Micrometres ? "um" : "mm";
        return $"{measurement.RealLength.Value:0.###} {suffix}";
    }

    private static string Display(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "Not specified" : value;

    private static string H(string? value) =>
        WebUtility.HtmlEncode(value ?? string.Empty);
}
