namespace UsbMicroscopeStudio.Services;

public sealed record InspectionSessionPaths(
    string SessionFolder,
    string CleanFramesFolder,
    string AnnotatedFramesFolder,
    string SidecarsFolder,
    string SessionJsonPath);
