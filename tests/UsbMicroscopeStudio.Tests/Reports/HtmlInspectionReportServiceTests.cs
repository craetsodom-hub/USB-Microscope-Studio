using UsbMicroscopeStudio.Models;
using UsbMicroscopeStudio.Models.Inspection;
using UsbMicroscopeStudio.Models.Sessions;
using UsbMicroscopeStudio.Services;

namespace UsbMicroscopeStudio.Tests.Reports;

public sealed class HtmlInspectionReportServiceTests : IDisposable
{
    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), "UsbMicroscopeStudioReportTests", Guid.NewGuid().ToString("N"));
    private readonly DateTimeOffset _generatedAt = new(2026, 7, 9, 16, 15, 30, TimeSpan.Zero);

    [Fact]
    public void Export_CreatesReportPathAndCopiesImages()
    {
        var sessionFolder = CreateSessionFolder();
        var cleanFrame = CreateImageFile(sessionFolder, "clean-frames", "clean.png");
        var annotatedFrame = CreateImageFile(sessionFolder, "annotated-frames", "annotated.png");
        var service = new HtmlInspectionReportService(() => _generatedAt);

        var result = service.Export(new InspectionSessionDocument
        {
            SessionName = "Board A",
            SessionFolderPath = sessionFolder,
            CleanFramePath = cleanFrame,
            AnnotatedFramePath = annotatedFrame
        });

        Assert.Equal(Path.Combine(sessionFolder, "reports", "report-20260709-161530.html"), result.ReportPath);
        Assert.True(File.Exists(result.ReportPath));
        Assert.Equal(2, result.CopiedImagePaths.Count);
        Assert.All(result.CopiedImagePaths, path => Assert.True(File.Exists(path)));

        var html = File.ReadAllText(result.ReportPath);
        Assert.Contains("assets/clean-frame.png", html);
        Assert.Contains("assets/annotated-frame.png", html);
        Assert.DoesNotContain(cleanFrame, html);
    }

    [Fact]
    public void Export_EscapesUserText()
    {
        var sessionFolder = CreateSessionFolder();
        var service = new HtmlInspectionReportService(() => _generatedAt);

        var result = service.Export(new InspectionSessionDocument
        {
            SessionName = "<Project>",
            CustomerName = "A & B",
            Notes = "<script>alert('x')</script>",
            SessionFolderPath = sessionFolder,
            Annotations =
            [
                new InspectionAnnotation
                {
                    Tool = InspectionTool.Text,
                    Text = "<b>Pin & pad</b>",
                    Points = [new(0.1, 0.2)]
                }
            ]
        });

        var html = File.ReadAllText(result.ReportPath);
        Assert.Contains("&lt;Project&gt;", html);
        Assert.Contains("A &amp; B", html);
        Assert.Contains("&lt;script&gt;alert(&#39;x&#39;)&lt;/script&gt;", html);
        Assert.Contains("&lt;b&gt;Pin &amp; pad&lt;/b&gt;", html);
        Assert.DoesNotContain("<script>alert", html);
        Assert.DoesNotContain("<b>Pin", html);
    }

    [Fact]
    public void Export_RendersMissingImagesWithoutImageTags()
    {
        var sessionFolder = CreateSessionFolder();
        var service = new HtmlInspectionReportService(() => _generatedAt);

        var result = service.Export(new InspectionSessionDocument
        {
            SessionName = "No images",
            SessionFolderPath = sessionFolder,
            CleanFramePath = Path.Combine(sessionFolder, "missing-clean.png")
        });

        var html = File.ReadAllText(result.ReportPath);
        Assert.Empty(result.CopiedImagePaths);
        Assert.Contains("No image available", html);
        Assert.DoesNotContain("<img", html);
    }

    [Fact]
    public void Export_RendersAnnotationAndMeasurementTable()
    {
        var sessionFolder = CreateSessionFolder();
        var service = new HtmlInspectionReportService(() => _generatedAt);

        var result = service.Export(new InspectionSessionDocument
        {
            SessionName = "Measured board",
            SessionFolderPath = sessionFolder,
            CalibrationStatus = "Calibrated",
            CalibrationProfile = new CalibrationProfile
            {
                Name = "10x",
                CameraId = "demo://microscope",
                Format = new CameraFormat(640, 480, 30),
                UnitsPerPixel = 0.01
            },
            Annotations =
            [
                new InspectionAnnotation
                {
                    Tool = InspectionTool.Text,
                    Text = "Socket A",
                    Points = [new(0.1, 0.2)]
                },
                new InspectionAnnotation
                {
                    Tool = InspectionTool.Angle,
                    IsMeasurement = true,
                    Points = [new(0.1, 0.4), new(0.5, 0.5), new(0.8, 0.2)]
                }
            ],
            Measurements =
            [
                new MeasurementResult(42.25, 0.423, 87.5, InspectionUnits.Millimetres, true)
            ]
        });

        var html = File.ReadAllText(result.ReportPath);
        Assert.Contains("Calibrated (10x)", html);
        Assert.Contains("<td>Text</td>", html);
        Assert.Contains("Socket A", html);
        Assert.Contains("<td>Angle</td>", html);
        Assert.Contains("42.25", html);
        Assert.Contains("0.423 mm", html);
        Assert.Contains("87.5&#176;", html);
    }

    [Fact]
    public void Export_RequiresSavedSessionFolder()
    {
        var service = new HtmlInspectionReportService(() => _generatedAt);

        var exception = Assert.Throws<InvalidOperationException>(() => service.Export(new InspectionSessionDocument()));

        Assert.Contains("Save the inspection session", exception.Message);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }

    private string CreateSessionFolder()
    {
        var sessionFolder = Path.Combine(_tempRoot, "20260709-161530-board-a");
        Directory.CreateDirectory(sessionFolder);
        return sessionFolder;
    }

    private static string CreateImageFile(string sessionFolder, string folderName, string fileName)
    {
        var folder = Path.Combine(sessionFolder, folderName);
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, fileName);
        File.WriteAllBytes(path, [137, 80, 78, 71, 13, 10, 26, 10]);
        return path;
    }
}
