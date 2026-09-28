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

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private double _progress;

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
        if (IsBusy)
            return;

        IsBusy = true;

        try
        {
            Status = "Récupération du modpack...";

            var manifest =
                await _modpackService.GetManifestAsync();

            Status =
                $"Modpack : {manifest.Name} | " +
                $"Version : {manifest.Version} | " +
                $"Mods : {manifest.Mods.Count}";

            Status = "Synchronisation des mods...";

            Progress = 0;

            await _modpackService.SynchronizeModsAsync(
                manifest,
                (fileName, currentMod, totalMods, currentBytes, totalBytes) =>
                {
                    // Mod déjà présent et valide
                    if (currentBytes == 1 && totalBytes == 1)
                    {
                        Progress = 100;

                        Status =
                            $"✓ {fileName} déjà à jour\n" +
                            $"Mod {currentMod}/{totalMods}";

                        return;
                    }

                    // Téléchargement avec taille connue
                    if (totalBytes > 0)
                    {
                        Progress =
                            (double)currentBytes /
                            totalBytes *
                            100;

                        var currentMb =
                            currentBytes / 1024.0 / 1024.0;

                        var totalMb =
                            totalBytes / 1024.0 / 1024.0;

                        Status =
                            $"Téléchargement de {fileName}\n" +
                            $"Mod {currentMod}/{totalMods}\n" +
                            $"{currentMb:0.0} / {totalMb:0.0} Mo";
                    }
                    else
                    {
                        Status =
                            $"Téléchargement de {fileName}\n" +
                            $"Mod {currentMod}/{totalMods}\n" +
                            $"{currentBytes / 1024 / 1024} Mo";
                    }
                });

            Progress = 100;
            Status = "Modpack prêt !";


            Status = "Lancement de Minecraft...";

            await _minecraftService.LaunchAsync();
        }
        catch (Exception ex)
        {
            Status = $"Erreur : {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}