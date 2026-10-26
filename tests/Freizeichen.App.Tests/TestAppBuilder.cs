using Avalonia;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(Freizeichen.App.Tests.TestAppBuilder))]

namespace Freizeichen.App.Tests;

internal static class TestAppBuilder
{
    // UseHeadlessDrawing = false keeps the real Skia text backend, and Inter is the font the app
    // ships with, so PNG captures for pull requests look like the app.
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<FreizeichenApp>()
            .UseSkia()
            .WithInterFont()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
