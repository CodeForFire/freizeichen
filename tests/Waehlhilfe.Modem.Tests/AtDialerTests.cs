using Microsoft.Extensions.Time.Testing;
using Waehlhilfe.Domain;

namespace Waehlhilfe.Modem.Tests;

public sealed class AtDialerTests
{
    private const string Port = "COM7";

    private readonly FakeTimeProvider _time = new();

    private static DialNumber Number(string link = "tel:0891234567")
    {
        Assert.True(TelUri.TryParse(link, out var uri));
        return DialNumber.From(uri, DialSettings.Default);
    }

    private AtDialer Dialer(IAtChannel channel) => new(new FakeChannelFactory(channel), _time);

    [Fact]
    public async Task Dials_the_number_the_way_the_dl500a_answered_it()
    {
        var phone = ScriptedAtChannel.Dl500a();

        var result = await Dialer(phone).DialAsync(Port, Number(), TestContext.Current.CancellationToken);

        Assert.Equal(DialResult.Dialled, result);
        Assert.Equal(["AT", "ATD 0891234567"], phone.Written);
    }

    [Fact]
    public async Task Opens_the_configured_port()
    {
        var factory = new FakeChannelFactory(ScriptedAtChannel.Dl500a());

        await new AtDialer(factory, _time).DialAsync(Port, Number(), TestContext.Current.CancellationToken);

        Assert.Equal([Port], factory.Opened);
    }

    [Fact]
    public async Task Closes_the_port_after_dialling()
    {
        var phone = ScriptedAtChannel.Dl500a();

        await Dialer(phone).DialAsync(Port, Number(), TestContext.Current.CancellationToken);

        Assert.True(phone.IsDisposed);
    }

    [Fact]
    public async Task Counts_the_call_as_dialled_when_connect_never_follows_ok()
    {
        var phone = new ScriptedAtChannel().Answer("AT", "OK").Answer("ATD 0891234567", "OK");

        var dial = Dialer(phone).DialAsync(Port, Number(), TestContext.Current.CancellationToken);
        _time.Advance(AtDialer.ConnectGrace);

        Assert.Equal(DialResult.Dialled, await dial);
        Assert.True(phone.IsDisposed);
    }

    [Fact]
    public async Task Never_sends_ATD_before_the_phone_answered_AT()
    {
        var phone = new ScriptedAtChannel();

        var dial = Dialer(phone).DialAsync(Port, Number(), TestContext.Current.CancellationToken);
        _time.Advance(AtDialer.ConnectTimeout);

        Assert.Equal(DialResult.NoResponse, await dial);
        Assert.Equal(["AT"], phone.Written);
        Assert.True(phone.IsDisposed);
    }

    [Fact]
    public async Task Waits_the_full_connect_timeout_for_the_bluetooth_link()
    {
        var phone = new ScriptedAtChannel();

        var dial = Dialer(phone).DialAsync(Port, Number(), TestContext.Current.CancellationToken);
        _time.Advance(AtDialer.ConnectTimeout - TimeSpan.FromMilliseconds(1));

        Assert.False(dial.IsCompleted);
        _time.Advance(TimeSpan.FromMilliseconds(1));
        Assert.Equal(DialResult.NoResponse, await dial);
    }

    [Fact]
    public async Task Reports_no_response_when_the_phone_never_answers_the_dial()
    {
        var phone = new ScriptedAtChannel().Answer("AT", "OK");

        var dial = Dialer(phone).DialAsync(Port, Number(), TestContext.Current.CancellationToken);
        _time.Advance(AtDialer.ResponseTimeout);

        Assert.Equal(DialResult.NoResponse, await dial);
    }

    [Fact]
    public async Task Reports_no_response_when_the_link_drops()
    {
        var phone = new ScriptedAtChannel();
        phone.Hangup();

        var result = await Dialer(phone).DialAsync(Port, Number(), TestContext.Current.CancellationToken);

        Assert.Equal(DialResult.NoResponse, result);
    }

    [Fact]
    public async Task Reports_no_response_when_the_link_breaks_while_reading()
    {
        var phone = new ScriptedAtChannel().Answer("AT", "OK");
        phone.Break(new IOException("The device is not connected."));

        var result = await Dialer(phone).DialAsync(Port, Number(), TestContext.Current.CancellationToken);

        Assert.Equal(DialResult.NoResponse, result);
        Assert.True(phone.IsDisposed);
    }

    [Fact]
    public async Task Reports_no_response_when_the_link_breaks_while_writing()
    {
        var phone = new ScriptedAtChannel { WriteFailure = new IOException("The semaphore timeout period has expired.") };

        var result = await Dialer(phone).DialAsync(Port, Number(), TestContext.Current.CancellationToken);

        Assert.Equal(DialResult.NoResponse, result);
        Assert.True(phone.IsDisposed);
    }

    [Theory]
    [InlineData("ERROR", DialResult.Rejected)]
    [InlineData("+CME ERROR: 3", DialResult.Rejected)]
    [InlineData("BUSY", DialResult.LineBusy)]
    [InlineData("NO CARRIER", DialResult.NoCarrier)]
    [InlineData("NO DIALTONE", DialResult.NoDialTone)]
    [InlineData("NO ANSWER", DialResult.NoCarrier)]
    public async Task Maps_the_phones_final_answer(string answer, DialResult expected)
    {
        var phone = new ScriptedAtChannel().Answer("AT", "OK").Answer("ATD 0891234567", "ATD 0891234567", answer);

        var result = await Dialer(phone).DialAsync(Port, Number(), TestContext.Current.CancellationToken);

        Assert.Equal(expected, result);
        Assert.True(phone.IsDisposed);
    }

    [Fact]
    public async Task Skips_unsolicited_lines_while_waiting()
    {
        var phone = new ScriptedAtChannel()
            .Answer("AT", string.Empty, "RING", "CLIP:EXTERN", "OK")
            .Answer("ATD 0891234567", "OK", "CONNECT");

        var result = await Dialer(phone).DialAsync(Port, Number(), TestContext.Current.CancellationToken);

        Assert.Equal(DialResult.Dialled, result);
    }

    [Fact]
    public async Task Rejects_when_the_phone_refuses_AT()
    {
        var phone = new ScriptedAtChannel().Answer("AT", "ERROR");

        var result = await Dialer(phone).DialAsync(Port, Number(), TestContext.Current.CancellationToken);

        Assert.Equal(DialResult.Rejected, result);
        Assert.Equal(["AT"], phone.Written);
    }

    [Theory]
    [InlineData(PortFailure.Missing, DialResult.PortMissing)]
    [InlineData(PortFailure.Busy, DialResult.PortBusy)]
    [InlineData(PortFailure.Unreachable, DialResult.NoResponse)]
    public async Task Reports_a_port_that_cannot_be_opened(PortFailure failure, DialResult expected)
    {
        var dialer = new AtDialer(new FakeChannelFactory(new PortUnavailableException(Port, failure)), _time);

        var result = await dialer.DialAsync(Port, Number(), TestContext.Current.CancellationToken);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Reports_a_missing_port_without_opening_anything(string port)
    {
        var factory = new FakeChannelFactory(ScriptedAtChannel.Dl500a());

        var result = await new AtDialer(factory, _time).DialAsync(port, Number(), TestContext.Current.CancellationToken);

        Assert.Equal(DialResult.PortNotSet, result);
        Assert.Empty(factory.Opened);
    }

    [Fact]
    public async Task Cancelling_closes_the_port_and_throws()
    {
        var phone = new ScriptedAtChannel();
        using var cancel = new CancellationTokenSource();

        var dial = Dialer(phone).DialAsync(Port, Number(), cancel.Token);
        await cancel.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => dial);
        Assert.True(phone.IsDisposed);
    }

    [Fact]
    public async Task Probe_sends_only_AT()
    {
        var phone = ScriptedAtChannel.Dl500a();

        var result = await Dialer(phone).ProbeAsync(Port, TestContext.Current.CancellationToken);

        Assert.Equal(DialResult.Answered, result);
        Assert.Equal(["AT"], phone.Written);
        Assert.True(phone.IsDisposed);
    }

    [Fact]
    public async Task Probe_reports_no_response_from_a_silent_phone()
    {
        var phone = new ScriptedAtChannel();

        var probe = Dialer(phone).ProbeAsync(Port, TestContext.Current.CancellationToken);
        _time.Advance(AtDialer.ConnectTimeout);

        Assert.Equal(DialResult.NoResponse, await probe);
    }
}
