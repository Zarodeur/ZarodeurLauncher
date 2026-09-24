using CommunityToolkit.Mvvm.ComponentModel;

namespace ZarodeurLauncher.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _status = "Prêt";
}