namespace UsbMicroscopeStudio.Models.Reports;

public sealed record HtmlInspectionReportResult(
    string ReportPath,
    string ReportsFolder,
    IReadOnlyList<string> CopiedImagePaths);
