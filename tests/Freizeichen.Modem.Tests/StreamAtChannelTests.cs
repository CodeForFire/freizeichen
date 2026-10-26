using System.Text;

namespace Freizeichen.Modem.Tests;

public sealed class StreamAtChannelTests
{
    private static (StreamAtChannel Channel, DuplexStream Stream) Over(string phoneSays)
    {
        var stream = new DuplexStream(Encoding.ASCII.GetBytes(phoneSays));
        return (new StreamAtChannel(stream), stream);
    }

    [Fact]
    public async Task Terminates_a_command_with_cr_lf()
    {
        var (channel, stream) = Over(string.Empty);
        await using var _ = channel;

        await channel.WriteLineAsync("ATD 0891234567", TestContext.Current.CancellationToken);

        Assert.Equal("ATD 0891234567\r\n", stream.Written);
    }

    [Theory]
    [InlineData("ATD 089\r\nATH")]
    [InlineData("ATD 089\n")]
    [InlineData("ATD 089\0")]
    [InlineData("ATD 08ä9")]
    public async Task Refuses_to_send_anything_but_one_printable_ascii_line(string line)
    {
        var (channel, stream) = Over(string.Empty);
        await using var _ = channel;

        await Assert.ThrowsAsync<ArgumentException>(() => channel.WriteLineAsync(line, TestContext.Current.CancellationToken));
        Assert.Equal(string.Empty, stream.Written);
    }

    [Fact]
    public async Task Splits_the_dl500a_answer_into_lines()
    {
        var (channel, _) = Over("ATD 0891234567\r\nOK\r\n\r\nCLIP:EXTERN\r\n\r\nCONNECT\r\n");
        await using var __ = channel;
        var lines = new List<string?>();

        for (var i = 0; i < 7; i++)
        {
            lines.Add(await channel.ReadLineAsync(TestContext.Current.CancellationToken));
        }

        Assert.Equal(["ATD 0891234567", "OK", string.Empty, "CLIP:EXTERN", string.Empty, "CONNECT", null], lines);
    }

    [Fact]
    public async Task Accepts_a_bare_cr_as_line_end()
    {
        var (channel, _) = Over("AT\rOK\r");
        await using var __ = channel;

        Assert.Equal("AT", await channel.ReadLineAsync(TestContext.Current.CancellationToken));
        Assert.Equal("OK", await channel.ReadLineAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Disposes_the_port_it_owns()
    {
        var owner = new DisposeProbe();
        var channel = new StreamAtChannel(new DuplexStream([]), owner);

        await channel.DisposeAsync();

        Assert.True(owner.IsDisposed);
    }

    private sealed class DisposeProbe : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Dispose() => IsDisposed = true;
    }
}

/// <summary>Reads what the phone "says" and records what was written to it.</summary>
internal sealed class DuplexStream(byte[] incoming) : Stream
{
    private readonly MemoryStream _in = new(incoming);
    private readonly MemoryStream _out = new();

    public string Written => Encoding.ASCII.GetString(_out.ToArray());

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => true;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count) => _in.Read(buffer, offset, count);

    public override void Write(byte[] buffer, int offset, int count) => _out.Write(buffer, offset, count);

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();
}
