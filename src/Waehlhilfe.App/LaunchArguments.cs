namespace Waehlhilfe.App;

internal static class LaunchArguments
{
    /// <summary>
    /// The <c>tel:</c> link Windows and Linux pass on the command line when a link is clicked,
    /// or <c>null</c> when Wählhilfe was started on its own. macOS delivers links as an Apple
    /// event instead, which arrives through <c>IActivatableLifetime</c>.
    /// </summary>
    public static string? TelLink(IEnumerable<string> args) =>
        args.FirstOrDefault(a => a.StartsWith("tel:", StringComparison.OrdinalIgnoreCase));
}
