using Freizeichen.AppLogic.ViewModels;
using Freizeichen.Domain;
using Freizeichen.Modem;

namespace Freizeichen.AppLogic.Tests;

public sealed class SettingsViewModelTests
{
    private readonly FakeDialer _dialer = new();
    private readonly FakeRegistration _registration = new();
    private readonly FakeSettingsStore _store = new(DialSettings.Default with { PortName = "COM7" });
    private IReadOnlyList<string> _ports = ["COM3", "COM7"];

    private SettingsViewModel ViewModel() => new(_store, _dialer, () => _ports, _registration);

    [Fact]
    public void Shows_the_stored_settings()
    {
        var vm = ViewModel();

        Assert.Equal(["COM3", "COM7"], vm.Ports);
        Assert.Equal("COM7", vm.SelectedPort);
        Assert.Equal("49", vm.HomeCountryCode);
        Assert.Equal(string.Empty, vm.OutsideLinePrefix);
    }

    [Fact]
    public void Keeps_showing_a_stored_port_that_is_not_present_now()
    {
        _ports = ["COM3"];

        var vm = ViewModel();

        Assert.Equal("COM7", vm.SelectedPort);
        Assert.Contains("COM7", vm.Ports);
    }

    [Fact]
    public void Picks_up_ports_that_appeared_after_pairing()
    {
        var vm = ViewModel();
        _ports = ["COM3", "COM7", "COM9"];

        vm.RefreshPortsCommand.Execute(null);

        Assert.Equal(["COM3", "COM7", "COM9"], vm.Ports);
        Assert.Equal("COM7", vm.SelectedPort);
    }

    [Fact]
    public void Saves_valid_settings()
    {
        var vm = ViewModel();
        vm.SelectedPort = "COM3";
        vm.HomeCountryCode = "43";
        vm.OutsideLinePrefix = "0";

        vm.SaveCommand.Execute(null);

        Assert.Equal(new DialSettings("COM3", "43", "0"), _store.Stored);
        Assert.Equal("Gespeichert.", vm.Status);
        Assert.False(vm.IsError);
    }

    [Theory]
    [InlineData("4;", "")]
    [InlineData("49", "0;ATH")]
    [InlineData("", "")]
    [InlineData("49", "*21*")]
    public void Refuses_to_save_invalid_settings(string country, string prefix)
    {
        var vm = ViewModel();
        vm.HomeCountryCode = country;
        vm.OutsideLinePrefix = prefix;

        vm.SaveCommand.Execute(null);

        Assert.Equal(0, _store.Saves);
        Assert.True(vm.IsError);
    }

    [Fact]
    public async Task Test_connection_only_probes()
    {
        var vm = ViewModel();
        _dialer.Result = DialResult.Answered;

        await vm.TestConnectionCommand.ExecuteAsync(null);

        Assert.Equal(["COM7"], _dialer.Probed);
        Assert.Empty(_dialer.Dialled);
        Assert.Equal("Telefon antwortet. Alles bereit.", vm.Status);
    }

    [Fact]
    public async Task Test_connection_uses_the_port_shown_even_before_saving()
    {
        var vm = ViewModel();
        vm.SelectedPort = "COM3";

        await vm.TestConnectionCommand.ExecuteAsync(null);

        Assert.Equal(["COM3"], _dialer.Probed);
    }

    [Fact]
    public async Task Test_call_dials_the_entered_number_with_the_shown_settings()
    {
        var vm = ViewModel();
        vm.OutsideLinePrefix = "0";
        vm.TestNumber = "0151 1234567";

        await vm.TestCallCommand.ExecuteAsync(null);

        Assert.Equal([("COM7", "001511234567")], _dialer.Dialled);
        Assert.Equal(DialMessages.For(DialResult.Dialled, "COM7"), vm.Status);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("*21*0151#")]
    public async Task Test_call_refuses_what_is_not_a_number(string number)
    {
        var vm = ViewModel();
        vm.TestNumber = number;

        await vm.TestCallCommand.ExecuteAsync(null);

        Assert.Empty(_dialer.Dialled);
        Assert.True(vm.IsError);
        Assert.Equal("Bitte eine Rufnummer eingeben.", vm.Status);
    }

    [Fact]
    public async Task Makes_freizeichen_the_tel_handler()
    {
        var vm = ViewModel();
        Assert.False(vm.IsDefaultHandler);

        await vm.MakeDefaultHandlerCommand.ExecuteAsync(null);

        Assert.Equal(1, _registration.Requests);
        Assert.True(vm.IsDefaultHandler);
    }

    [Fact]
    public async Task Reports_when_the_os_did_not_make_it_the_handler()
    {
        _registration.Failure = new InvalidOperationException("denied");
        var vm = ViewModel();

        await vm.MakeDefaultHandlerCommand.ExecuteAsync(null);

        Assert.True(vm.IsError);
        Assert.False(vm.IsDefaultHandler);
        Assert.Equal("Freizeichen konnte nicht als Standard für tel:-Links eingetragen werden.", vm.Status);
    }

    [Fact]
    public async Task Re_reads_the_handler_state_after_the_user_returns_from_the_os_dialog()
    {
        _registration.BecomesDefault = false;
        var vm = ViewModel();

        await vm.MakeDefaultHandlerCommand.ExecuteAsync(null);
        _registration.IsDefault = true;
        vm.RefreshDefaultHandlerCommand.Execute(null);

        Assert.True(vm.IsDefaultHandler);
    }

    // When the answer has not changed, the line under TEL:-LINKS stays as it was; the status is the
    // only sign the check ran at all.
    [Theory]
    [InlineData(true, "Geprüft: Freizeichen öffnet tel:-Links.")]
    [InlineData(false, "Geprüft: Ein anderes Programm öffnet tel:-Links.")]
    public void Check_answers_in_the_status_even_when_nothing_changed(bool isDefault, string status)
    {
        _registration.IsDefault = isDefault;
        var vm = ViewModel();

        vm.RefreshDefaultHandlerCommand.Execute(null);

        Assert.Equal(status, vm.Status);
        Assert.False(vm.IsError);
    }
}
