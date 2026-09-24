using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZarodeurLauncher.Services;

namespace ZarodeurLauncher.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly MinecraftPathService _minecraftPathService;
    private readonly MinecraftService _minecraftService;

    [ObservableProperty]
    private string _status = "Prêt";

    public MainViewModel()
    {
        _minecraftPathService = new MinecraftPathService();

        _minecraftService = new MinecraftService(
            _minecraftPathService);
    }

    [RelayCommand]
    private async Task PrepareMinecraft()
    {
        try
        {
            Status = "Préparation de Minecraft...";

            await _minecraftService.LaunchAsync();

            Status = "Minecraft lancé !";
        }
        catch (Exception ex)
        {
            Status = $"Erreur : {ex.Message}";
        }
    }
}