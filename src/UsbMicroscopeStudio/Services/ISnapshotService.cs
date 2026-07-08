using System.Windows.Media.Imaging;

namespace UsbMicroscopeStudio.Services;

public interface ISnapshotService
{
    string DefaultSnapshotDirectory { get; }

    string SaveSnapshot(BitmapSource frame, string snapshotDirectory);
}
