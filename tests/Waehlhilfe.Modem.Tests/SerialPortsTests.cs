namespace Waehlhilfe.Modem.Tests;

public sealed class SerialPortsTests
{
    [Fact]
    public void Lists_bluetooth_ports_on_macos_by_their_callout_device()
    {
        var ports = SerialPorts.Filter(
            ["/dev/cu.DL500A", "/dev/tty.DL500A", "/dev/cu.Bluetooth-Incoming-Port", "/dev/cu.debug-console", "/dev/tty.Bluetooth-Incoming-Port"],
            isMacOS: true);

        Assert.Equal(["/dev/cu.DL500A"], ports);
    }

    [Fact]
    public void Lists_windows_com_ports_in_numeric_order()
    {
        var ports = SerialPorts.Filter(["COM10", "COM3", "COM7", "COM3"], isMacOS: false);

        Assert.Equal(["COM3", "COM7", "COM10"], ports);
    }

    [Fact]
    public void Lists_linux_rfcomm_ports_first()
    {
        var ports = SerialPorts.Filter(["/dev/ttyS0", "/dev/rfcomm0", "/dev/ttyUSB0"], isMacOS: false);

        Assert.Equal(["/dev/rfcomm0", "/dev/ttyS0", "/dev/ttyUSB0"], ports);
    }

    [Theory]
    [InlineData(typeof(FileNotFoundException), PortFailure.Missing)]
    [InlineData(typeof(ArgumentException), PortFailure.Missing)]
    [InlineData(typeof(UnauthorizedAccessException), PortFailure.Busy)]
    [InlineData(typeof(IOException), PortFailure.Unreachable)]
    [InlineData(typeof(TimeoutException), PortFailure.Unreachable)]
    public void Classifies_why_a_port_did_not_open(Type exception, PortFailure expected)
    {
        var error = (Exception)Activator.CreateInstance(exception)!;

        Assert.Equal(expected, SerialPorts.Classify(error));
    }

    [Fact]
    public void Leaves_unrelated_exceptions_alone()
    {
        Assert.Null(SerialPorts.Classify(new InvalidOperationException()));
    }
}
