using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZarodeurLauncher.Services;

namespace ZarodeurLauncher.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly MinecraftPathService _minecraftPathService;

    [ObservableProperty]
    private string _status = "Prêt";

    public MainViewModel()
    {
        _minecraftPathService = new MinecraftPathService();
    }

    [RelayCommand]
    private void PrepareMinecraft()
    {
        Status = $"Dossier Minecraft : {_minecraftPathService.MinecraftPath}";
    }
}