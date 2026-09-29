using System;
using Avalonia.Controls;
using ZarodeurLauncher.ViewModels;

namespace ZarodeurLauncher.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Opened += MainWindow_Opened;
    }

    private async void MainWindow_Opened(
        object? sender,
        EventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            await viewModel.AuthenticateMicrosoftAsync();

            await viewModel.CheckModpackAsync();
        }
    }

    // ============================================================
    // CONFIRMATION DE SUPPRESSION DES LOGS
    // ============================================================

    public async void ShowClearLogsConfirmation()
    {
        if (DataContext is not MainViewModel viewModel)
            return;

        var dialog =
            new ConfirmDialog();

        await dialog.ShowDialog(this);

        if (dialog.Confirmed)
        {
            viewModel.ConfirmClearLogs();
        }
    }
}