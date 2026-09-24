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
    private readonly ModpackService _modpackService;

    [ObservableProperty]
    private string _status = "Prêt";

    public MainViewModel()
    {
        _minecraftPathService = new MinecraftPathService();

        _minecraftService = new MinecraftService(
            _minecraftPathService);

        _modpackService = new ModpackService(
            _minecraftPathService);
    }

    [RelayCommand]
    private async Task PrepareMinecraft()
    {
        try
        {
            Status = "Récupération du modpack...";

            var manifest =
                await _modpackService.GetManifestAsync();

            Status =
                $"Modpack : {manifest.Name} | " +
                $"Version : {manifest.Version} | " +
                $"Mods : {manifest.Mods.Count}";

            foreach (var mod in manifest.Mods)
            {
                Status = $"Vérification de {mod.File}...";

                var downloaded =
                    await _modpackService.DownloadModAsync(mod);

                if (downloaded)
                {
                    Status =
                        $"{mod.File} téléchargé et vérifié.";
                }
                else
                {
                    Status =
                        $"{mod.File} déjà à jour.";
                }
            }

            Status = "Modpack prêt !";
        }
        catch (Exception ex)
        {
            Status = $"Erreur : {ex.Message}";
        }
    }
}