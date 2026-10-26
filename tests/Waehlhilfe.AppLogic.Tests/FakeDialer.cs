using Waehlhilfe.Domain;
using Waehlhilfe.Modem;

namespace Waehlhilfe.AppLogic.Tests;

internal sealed class FakeDialer : IDialer
{
    private TaskCompletionSource<DialResult>? _pending;

    public DialResult Result { get; set; } = DialResult.Dialled;

    public List<(string Port, string Digits)> Dialled { get; } = [];

    public List<string> Probed { get; } = [];

    public CancellationToken LastToken { get; private set; }

    /// <summary>Makes the next dial or probe wait until <see cref="Complete"/>.</summary>
    public void Hold() => _pending = new TaskCompletionSource<DialResult>(TaskCreationOptions.RunContinuationsAsynchronously);

    public void Complete(DialResult result) => _pending?.TrySetResult(result);

    public Task<DialResult> DialAsync(string portName, DialNumber number, CancellationToken cancellationToken = default)
    {
        Dialled.Add((portName, number.Digits));
        return ResultAsync(cancellationToken);
    }

    public Task<DialResult> ProbeAsync(string portName, CancellationToken cancellationToken = default)
    {
        Probed.Add(portName);
        return ResultAsync(cancellationToken);
    }

    private Task<DialResult> ResultAsync(CancellationToken cancellationToken)
    {
        LastToken = cancellationToken;
        return _pending is null ? Task.FromResult(Result) : _pending.Task.WaitAsync(cancellationToken);
    }
}

internal sealed class FakeSettingsStore(DialSettings settings) : ISettingsStore
{
    public DialSettings Stored { get; private set; } = settings;

    public int Saves { get; private set; }

    public DialSettings Load() => Stored;

    public void Save(DialSettings settings)
    {
        Stored = settings;
        Saves++;
    }
}

internal sealed class FakeRegistration : ITelHandlerRegistration
{
    public bool IsDefault { get; set; }

    public bool BecomesDefault { get; set; } = true;

    public Exception? Failure { get; set; }

    public int Requests { get; private set; }

    public bool IsDefaultHandler() => IsDefault;

    public Task MakeDefaultAsync(CancellationToken cancellationToken = default)
    {
        Requests++;
        if (Failure is not null)
        {
            throw Failure;
        }

        IsDefault = BecomesDefault;
        return Task.CompletedTask;
    }
}
