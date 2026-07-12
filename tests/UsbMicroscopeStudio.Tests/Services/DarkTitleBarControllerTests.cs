using UsbMicroscopeStudio.Services;
using Xunit;

namespace UsbMicroscopeStudio.Tests.Services;

public sealed class DarkTitleBarControllerTests
{
    [Fact]
    public void Apply_UsesNativeDarkModeAndGraphiteCaptionAttributes()
    {
        var interop = new FakeTitleBarInterop();
        var controller = new DarkTitleBarController(interop);

        var applied = controller.Apply((nint)42);

        Assert.True(applied);
        Assert.Equal(
            new[]
            {
                (42, 20, 1),
                (42, 35, 0x001A130D),
                (42, 36, 0x00EEE8DD),
                (42, 34, 0x00433626)
            },
            interop.Calls);
    }

    [Fact]
    public void Apply_FallsBackToLegacyDarkModeAttribute()
    {
        var interop = new FakeTitleBarInterop(attribute => attribute == 20 ? -1 : 0);
        var controller = new DarkTitleBarController(interop);

        var applied = controller.Apply((nint)42);

        Assert.True(applied);
        Assert.Equal((42, 20, 1), interop.Calls[0]);
        Assert.Equal((42, 19, 1), interop.Calls[1]);
        Assert.Contains((42, 35, 0x001A130D), interop.Calls);
    }

    [Fact]
    public void Apply_DoesNothingWithoutAWindowHandle()
    {
        var interop = new FakeTitleBarInterop();
        var controller = new DarkTitleBarController(interop);

        Assert.False(controller.Apply(0));
        Assert.Empty(interop.Calls);
    }

    private sealed class FakeTitleBarInterop(Func<int, int>? resultFactory = null) : IWindowTitleBarInterop
    {
        public List<(int Handle, int Attribute, int Value)> Calls { get; } = [];

        public int SetWindowAttribute(nint windowHandle, int attribute, int value)
        {
            Calls.Add(((int)windowHandle, attribute, value));
            return resultFactory?.Invoke(attribute) ?? 0;
        }
    }
}
