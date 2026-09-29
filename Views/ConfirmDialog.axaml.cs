using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ZarodeurLauncher.Views;

public partial class ConfirmDialog : Window
{
    public bool Confirmed { get; private set; }

    public ConfirmDialog()
    {
        InitializeComponent();
    }

    private void Cancel_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Confirmed = false;
        Close();
    }

    private void Confirm_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Confirmed = true;
        Close();
    }
}