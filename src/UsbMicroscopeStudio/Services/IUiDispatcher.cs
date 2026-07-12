namespace UsbMicroscopeStudio.Services;

public interface IUiDispatcher
{
    void Invoke(Action action);

    void BeginInvoke(Action action);
}
