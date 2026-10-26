using Waehlhilfe.Modem;

namespace Waehlhilfe.AppLogic.Tests;

public sealed class DialMessagesTests
{
    [Fact]
    public void Every_result_has_a_german_message()
    {
        foreach (var result in Enum.GetValues<DialResult>())
        {
            Assert.False(string.IsNullOrWhiteSpace(DialMessages.For(result, "COM7")));
        }
    }

    [Fact]
    public void No_response_names_the_audio_connection_trap()
    {
        Assert.Contains("Audiogerät", DialMessages.For(DialResult.NoResponse, "COM7"), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(DialResult.PortMissing)]
    [InlineData(DialResult.PortBusy)]
    public void Port_failures_name_the_port(DialResult result)
    {
        Assert.Contains("COM7", DialMessages.For(result, "COM7"), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(DialResult.Dialled, false)]
    [InlineData(DialResult.Answered, false)]
    [InlineData(DialResult.NoResponse, true)]
    [InlineData(DialResult.LineBusy, true)]
    public void Tells_success_from_failure(DialResult result, bool isError)
    {
        Assert.Equal(isError, DialMessages.IsError(result));
    }
}
