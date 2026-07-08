using System.Runtime.InteropServices;
using DirectShowLib;
using UsbMicroscopeStudio.Models;

namespace UsbMicroscopeStudio.Services;

public sealed class DirectShowCameraCatalog : ICameraCatalog
{
    public const string DemoCameraId = "demo://microscope";

    private static readonly CameraDevice DemoCamera = new(DemoCameraId, "Synthetic Microscope Feed", -1, true);

    private static readonly CameraFormat[] DemoFormats =
    [
        new(1280, 720, 30, "Demo"),
        new(1920, 1080, 30, "Demo"),
        new(640, 480, 60, "Demo")
    ];

    public Task<IReadOnlyList<CameraDevice>> GetCamerasAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run<IReadOnlyList<CameraDevice>>(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.Equals(Environment.GetEnvironmentVariable("USB_MICROSCOPE_STUDIO_DEMO_ONLY"), "1", StringComparison.OrdinalIgnoreCase))
            {
                return [DemoCamera];
            }

            var devices = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice)
                .Select((device, index) => new CameraDevice(device.DevicePath, device.Name, index))
                .Concat([DemoCamera])
                .ToList();

            return devices;
        }, cancellationToken);
    }

    public Task<IReadOnlyList<CameraFormat>> GetFormatsAsync(CameraDevice camera, CancellationToken cancellationToken = default)
    {
        if (camera.IsDemo)
        {
            return Task.FromResult<IReadOnlyList<CameraFormat>>(DemoFormats);
        }

        return Task.Run<IReadOnlyList<CameraFormat>>(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var device = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice)
                .Select((value, index) => new { value, index })
                .FirstOrDefault(item => item.index == camera.Index || item.value.DevicePath == camera.Id);

            if (device is null)
            {
                return DefaultFormats();
            }

            return ReadDirectShowFormats(device.value);
        }, cancellationToken);
    }

    private static IReadOnlyList<CameraFormat> ReadDirectShowFormats(DsDevice device)
    {
        IGraphBuilder? graph = null;
        ICaptureGraphBuilder2? captureGraph = null;
        IBaseFilter? sourceFilter = null;

        try
        {
            graph = (IGraphBuilder)new FilterGraph();
            captureGraph = (ICaptureGraphBuilder2)new CaptureGraphBuilder2();
            DsError.ThrowExceptionForHR(captureGraph.SetFiltergraph(graph));

            var filterGuid = typeof(IBaseFilter).GUID;
            device.Mon.BindToObject(null!, null, ref filterGuid, out var source);
            sourceFilter = (IBaseFilter)source;
            DsError.ThrowExceptionForHR(graph.AddFilter(sourceFilter, "Video Capture"));

            var category = PinCategory.Capture;
            var mediaType = MediaType.Video;
            var streamConfigGuid = typeof(IAMStreamConfig).GUID;
            DsError.ThrowExceptionForHR(captureGraph.FindInterface(category, mediaType, sourceFilter, streamConfigGuid, out var streamConfigObject));

            var streamConfig = (IAMStreamConfig)streamConfigObject;
            DsError.ThrowExceptionForHR(streamConfig.GetNumberOfCapabilities(out var count, out var capabilitySize));

            var capabilitiesPointer = Marshal.AllocCoTaskMem(capabilitySize);
            try
            {
                var formats = new List<CameraFormat>();
                for (var i = 0; i < count; i++)
                {
                    DsError.ThrowExceptionForHR(streamConfig.GetStreamCaps(i, out var media, capabilitiesPointer));
                    try
                    {
                        var format = ConvertMediaType(media);
                        if (format is not null)
                        {
                            formats.Add(format);
                        }
                    }
                    finally
                    {
                        DsUtils.FreeAMMediaType(media);
                    }
                }

                return formats
                    .DistinctBy(format => (format.Width, format.Height, Math.Round(format.FramesPerSecond, 1)))
                    .OrderByDescending(format => format.Width * format.Height)
                    .ThenByDescending(format => format.FramesPerSecond)
                    .DefaultIfEmpty(DefaultFormats()[0])
                    .ToList();
            }
            finally
            {
                Marshal.FreeCoTaskMem(capabilitiesPointer);
            }
        }
        catch
        {
            return DefaultFormats();
        }
        finally
        {
            if (sourceFilter is not null)
            {
                graph?.RemoveFilter(sourceFilter);
                Marshal.ReleaseComObject(sourceFilter);
            }

            if (captureGraph is not null)
            {
                Marshal.ReleaseComObject(captureGraph);
            }

            if (graph is not null)
            {
                Marshal.ReleaseComObject(graph);
            }
        }
    }

    private static CameraFormat? ConvertMediaType(AMMediaType media)
    {
        if (media.formatPtr == IntPtr.Zero || media.formatType != FormatType.VideoInfo)
        {
            return null;
        }

        var videoInfo = Marshal.PtrToStructure<VideoInfoHeader>(media.formatPtr)!;
        var width = Math.Abs(videoInfo.BmiHeader.Width);
        var height = Math.Abs(videoInfo.BmiHeader.Height);
        var fps = videoInfo.AvgTimePerFrame > 0 ? 10_000_000d / videoInfo.AvgTimePerFrame : 30d;

        if (width <= 0 || height <= 0)
        {
            return null;
        }

        return new CameraFormat(width, height, Math.Round(fps, 1), media.subType.ToString());
    }

    private static IReadOnlyList<CameraFormat> DefaultFormats() =>
    [
        new(1280, 720, 30, "Default"),
        new(640, 480, 30, "Default")
    ];
}
