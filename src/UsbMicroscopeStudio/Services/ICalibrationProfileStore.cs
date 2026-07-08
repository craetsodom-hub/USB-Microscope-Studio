using UsbMicroscopeStudio.Models.Inspection;

namespace UsbMicroscopeStudio.Services;

public interface ICalibrationProfileStore
{
    IReadOnlyList<CalibrationProfile> Load();

    void Save(IReadOnlyList<CalibrationProfile> profiles);
}
