using Avalonia;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(Waehlhilfe.App.Tests.TestAppBuilder))]

namespace Waehlhilfe.App.Tests;

internal static class TestAppBuilder
{
    // UseHeadlessDrawing = false keeps the real Skia text backend, so PNG captures for pull
    // requests show real glyphs instead of the headless drawing stub.
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<WaehlhilfeApp>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
