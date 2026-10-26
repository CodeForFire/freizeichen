namespace Freizeichen.AppLogic;

/// <summary>Whether the OS opens <c>tel:</c> links with Freizeichen, and the way to make it so.</summary>
public interface ITelHandlerRegistration
{
    bool IsDefaultHandler();

    /// <summary>
    /// Makes Freizeichen the handler, or opens the OS dialog where the user does — Windows lets
    /// no program set it on its own. Throws when neither is possible.
    /// </summary>
    Task MakeDefaultAsync(CancellationToken cancellationToken = default);
}
