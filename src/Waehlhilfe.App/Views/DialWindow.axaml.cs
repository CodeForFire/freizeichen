using Avalonia.Controls;
using Waehlhilfe.AppLogic.ViewModels;

namespace Waehlhilfe.App.Views;

internal sealed partial class DialWindow : Window
{
    private DialViewModel? _viewModel;

    public DialWindow() => InitializeComponent();

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        _viewModel?.CloseRequested -= OnCloseRequested;
        _viewModel = DataContext as DialViewModel;
        _viewModel?.CloseRequested += OnCloseRequested;
    }

    private void OnCloseRequested(object? sender, EventArgs e) => Close();
}
