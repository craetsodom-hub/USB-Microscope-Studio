using System.Windows;
using System.Windows.Threading;

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

    public void BeginInvoke(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            action();
            return;
        }

        _ = dispatcher.BeginInvoke(action, DispatcherPriority.Render);
    }
}
