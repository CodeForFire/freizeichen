using Microsoft.Extensions.Time.Testing;
using Waehlhilfe.AppLogic.ViewModels;
using Waehlhilfe.Domain;
using Waehlhilfe.Modem;

namespace Waehlhilfe.AppLogic.Tests;

public sealed class DialViewModelTests
{
    private static readonly DialSettings Settings = DialSettings.Default with { PortName = "COM7" };

    private readonly FakeDialer _dialer = new();
    private readonly FakeTimeProvider _time = new();

    private DialViewModel ViewModel(string link, DialSettings? settings = null)
    {
        var vm = new DialViewModel(link, settings ?? Settings, _dialer, _time);
        _time.Advance(DialViewModel.ArmDelay);
        return vm;
    }

    // A tel: link can arrive while the Lagebuchführer is typing elsewhere. The window comes to
    // the front and takes focus, so an Enter meant for another program would confirm the call.
    [Fact]
    public async Task Ignores_a_confirm_that_arrives_before_the_window_is_armed()
    {
        var vm = new DialViewModel("tel:0891234567", Settings, _dialer, _time);

        Assert.False(vm.DialCommand.CanExecute(null));
        await vm.DialCommand.ExecuteAsync(null);
        _time.Advance(DialViewModel.ArmDelay - TimeSpan.FromMilliseconds(1));
        await vm.DialCommand.ExecuteAsync(null);

        Assert.Empty(_dialer.Dialled);
    }

    [Fact]
    public async Task Dials_once_armed()
    {
        var vm = new DialViewModel("tel:0891234567", Settings, _dialer, _time);
        var changed = 0;
        vm.DialCommand.CanExecuteChanged += (_, _) => changed++;

        _time.Advance(DialViewModel.ArmDelay);
        await vm.DialCommand.ExecuteAsync(null);

        Assert.True(changed > 0);
        Assert.Single(_dialer.Dialled);
    }

    [Fact]
    public void Shows_the_number_that_will_be_dialled()
    {
        var vm = ViewModel("tel:+49 89 1234567");

        Assert.Equal("0891234567", vm.Number);
        Assert.True(vm.CanDial);
        Assert.True(vm.DialCommand.CanExecute(null));
    }

    [Fact]
    public void Does_not_dial_until_confirmed()
    {
        _ = ViewModel("tel:0891234567");

        Assert.Empty(_dialer.Dialled);
    }

    [Fact]
    public async Task Dials_on_the_configured_port_when_confirmed()
    {
        var vm = ViewModel("tel:0891234567");

        await vm.DialCommand.ExecuteAsync(null);

        Assert.Equal([("COM7", "0891234567")], _dialer.Dialled);
        Assert.Equal("Gewählt. Gespräch am Telefon führen.", vm.Status);
        Assert.False(vm.IsError);
    }

    [Fact]
    public async Task Closes_itself_a_moment_after_dialling()
    {
        var vm = ViewModel("tel:0891234567");
        var closed = 0;
        vm.CloseRequested += (_, _) => closed++;

        await vm.DialCommand.ExecuteAsync(null);
        Assert.Equal(0, closed);
        _time.Advance(DialViewModel.CloseDelay);

        Assert.Equal(1, closed);
    }

    [Fact]
    public async Task Stays_open_and_shows_why_when_dialling_failed()
    {
        _dialer.Result = DialResult.NoResponse;
        var vm = ViewModel("tel:0891234567");
        var closed = 0;
        vm.CloseRequested += (_, _) => closed++;

        await vm.DialCommand.ExecuteAsync(null);
        _time.Advance(DialViewModel.CloseDelay);

        Assert.Equal(0, closed);
        Assert.True(vm.IsError);
        Assert.Equal(DialMessages.For(DialResult.NoResponse, "COM7"), vm.Status);
        Assert.True(vm.DialCommand.CanExecute(null));
    }

    [Fact]
    public async Task Cannot_dial_twice_while_a_dial_is_running()
    {
        _dialer.Hold();
        var vm = ViewModel("tel:0891234567");

        var dial = vm.DialCommand.ExecuteAsync(null);

        Assert.True(vm.IsBusy);
        Assert.Equal("Wähle …", vm.Status);
        Assert.False(vm.DialCommand.CanExecute(null));
        _dialer.Complete(DialResult.Dialled);
        await dial;
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public void Cancel_closes_without_dialling()
    {
        var vm = ViewModel("tel:0891234567");
        var closed = 0;
        vm.CloseRequested += (_, _) => closed++;

        vm.CancelCommand.Execute(null);

        Assert.Equal(1, closed);
        Assert.Empty(_dialer.Dialled);
    }

    [Fact]
    public async Task Cancel_during_a_dial_stops_it()
    {
        _dialer.Hold();
        var vm = ViewModel("tel:0891234567");
        var dial = vm.DialCommand.ExecuteAsync(null);

        vm.CancelCommand.Execute(null);
        await dial;

        Assert.True(_dialer.LastToken.IsCancellationRequested);
        Assert.False(vm.IsBusy);
    }

    [Theory]
    [InlineData("tel:0891234567%0D%0AATH")]
    [InlineData("tel:*21*015112345678%23")]
    [InlineData("mailto:someone@example.org")]
    [InlineData("")]
    public async Task Never_dials_a_link_that_is_not_a_plain_number(string link)
    {
        var vm = ViewModel(link);

        Assert.False(vm.CanDial);
        Assert.False(vm.DialCommand.CanExecute(null));
        Assert.True(vm.IsError);
        Assert.Equal("Kein gültiger tel:-Link – es wird nicht gewählt.", vm.Status);
        await vm.DialCommand.ExecuteAsync(null);
        Assert.Empty(_dialer.Dialled);
    }

    [Fact]
    public void Shows_no_number_for_a_rejected_link()
    {
        var vm = ViewModel("tel:0891234567%0D%0AATH");

        Assert.Equal(string.Empty, vm.Number);
    }

    [Fact]
    public void Says_where_to_set_the_phone_up_when_no_port_is_chosen()
    {
        var vm = ViewModel("tel:0891234567", DialSettings.Default);

        Assert.False(vm.CanDial);
        Assert.True(vm.IsError);
        Assert.Equal(DialMessages.For(DialResult.PortNotSet, string.Empty), vm.Status);
    }

    [Fact]
    public void Applies_the_outside_line_prefix_to_the_shown_number()
    {
        var vm = ViewModel("tel:0891234567", Settings with { OutsideLinePrefix = "0" });

        Assert.Equal("00891234567", vm.Number);
    }
}
