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
            await viewModel.CheckModpackAsync();
        }
    }
}