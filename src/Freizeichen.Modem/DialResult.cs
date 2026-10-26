namespace Freizeichen.Modem;

public enum DialResult
{
    /// <summary>The phone accepted <c>ATD</c> and is setting up the call.</summary>
    Dialled,

    /// <summary>The phone answered <c>AT</c>; returned by a probe only.</summary>
    Answered,

    /// <summary>No port has been chosen yet.</summary>
    PortNotSet,

    PortMissing,

    PortBusy,

    /// <summary>
    /// The port opened but the phone never answered. With a DL500A this is what an audio
    /// (Hands-Free) connection to the same PC does to the serial port.
    /// </summary>
    NoResponse,

    /// <summary><c>ERROR</c> or <c>+CME ERROR</c>.</summary>
    Rejected,

    LineBusy,

    NoCarrier,

    NoDialTone,
}
