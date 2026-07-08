using System.Windows.Media.Imaging;

namespace UsbMicroscopeStudio.Services;

public interface ISnapshotService
{
    string SaveSnapshot(BitmapSource frame);
}
