namespace UsbMicroscopeStudio.Models.Sessions;

public sealed record RecentSessionEntry
{
    public string SessionName { get; init; } = "Untitled inspection";

    public string SessionPath { get; init; } = string.Empty;

    public DateTimeOffset LastOpenedAt { get; init; } = DateTimeOffset.Now;

    public string DisplayName => string.IsNullOrWhiteSpace(SessionPath)
        ? SessionName
        : $"{SessionName} - {SessionPath}";
}
