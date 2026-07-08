using System.Windows;

namespace UsbMicroscopeStudio.Services;

public sealed class WpfUiDispatcher : IUiDispatcher
{
    public void Invoke(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            action();
            return;
        }

        dispatcher.Invoke(action);
    }
}
