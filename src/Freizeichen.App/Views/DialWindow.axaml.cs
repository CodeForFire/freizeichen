using Avalonia.Controls;
using Freizeichen.AppLogic.ViewModels;

namespace Freizeichen.App.Views;

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

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        WindowsTitleBar.ApplyDarkMode(this);
    }

    // However the window goes — title bar, or a newer link replacing it — a dial still running
    // behind it is cancelled, so no call is placed from a window nobody can see.
    protected override void OnClosed(EventArgs e)
    {
        if (_viewModel is { } viewModel)
        {
            viewModel.CloseRequested -= OnCloseRequested;
            if (viewModel.IsBusy)
            {
                viewModel.CancelCommand.Execute(null);
            }
        }

        base.OnClosed(e);
    }

    private void OnCloseRequested(object? sender, EventArgs e) => Close();
}
