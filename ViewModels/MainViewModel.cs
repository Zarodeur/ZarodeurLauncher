using System;
using System.Collections.ObjectModel;
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
    private readonly LauncherSettingsService _settingsService;

    [ObservableProperty]
    private string _status = "Prêt";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private string _modpackVersion = "Version inconnue";

    [ObservableProperty]
    private string _modpackStatus = "Vérification...";

    [ObservableProperty]
    private string _playButtonText = "JOUER";

    [ObservableProperty]
    private string _selectedRam;

    public ObservableCollection<string> RamOptions { get; } =
        new()
        {
            "4 Go",
            "6 Go",
            "8 Go",
            "10 Go",
            "12 Go",
            "14 Go",
            "16 Go"
        };

    public MainViewModel()
    {
        _minecraftPathService = new MinecraftPathService();

        _settingsService = new LauncherSettingsService();

        _selectedRam =
            $"{_settingsService.GetRam()} Go";

        _minecraftService = new MinecraftService(
            _minecraftPathService);

        _modpackService = new ModpackService(
            _minecraftPathService);
    }

    partial void OnSelectedRamChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        if (int.TryParse(
            value.Replace(" Go", ""),
            out var ram))
        {
            _settingsService.SaveRam(ram);
        }
    }

    public async Task CheckModpackAsync()
    {
        try
        {
            ModpackStatus = "Vérification...";

            var manifest =
                await _modpackService.GetManifestAsync();

            var installedVersion =
                _modpackService.GetInstalledVersion();

            ModpackVersion =
                $"Version disponible : {manifest.Version}";

            if (string.IsNullOrEmpty(installedVersion))
            {
                ModpackStatus =
                    "Installation requise";

                PlayButtonText =
                    "INSTALLER";
            }
            else if (installedVersion != manifest.Version)
            {
                ModpackStatus =
                    $"⚠ Mise à jour disponible • {installedVersion} → {manifest.Version}";

                PlayButtonText =
                    "METTRE À JOUR";
            }
            else
            {
                ModpackStatus =
                    "✓ Modpack à jour";

                PlayButtonText =
                    "JOUER";
            }
        }
        catch (Exception ex)
        {
            ModpackStatus =
                $"Impossible de vérifier le modpack : {ex.Message}";
        }
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

            var installedVersion =
                _modpackService.GetInstalledVersion();

            ModpackVersion =
                $"Version disponible : {manifest.Version}";

            if (string.IsNullOrEmpty(installedVersion))
            {
                Status =
                    $"Première installation du modpack\n" +
                    $"Version disponible : {manifest.Version}";
            }
            else if (installedVersion != manifest.Version)
            {
                Status =
                    $"Mise à jour du modpack\n" +
                    $"{installedVersion} → {manifest.Version}";
            }
            else
            {
                Status =
                    $"Modpack à jour\n" +
                    $"Version : {manifest.Version}";
            }

            Status = "Synchronisation des mods...";

            Progress = 0;

            await _modpackService.SynchronizeModsAsync(
                manifest,
                (fileName, currentMod, totalMods, currentBytes, totalBytes) =>
                {
                    if (currentBytes == 1 && totalBytes == 1)
                    {
                        Progress = 100;

                        Status =
                            $"✓ {fileName} déjà à jour\n" +
                            $"Mod {currentMod}/{totalMods}";

                        return;
                    }

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

            _modpackService.SaveInstalledVersion(
                manifest.Version);

            PlayButtonText = "JOUER";

            ModpackVersion =
                $"Version installée : {manifest.Version}";

            ModpackStatus =
                "✓ Modpack à jour";

            Status =
                $"Modpack prêt ! Version {manifest.Version}";

            Status =
                "Lancement de Minecraft...";

            var ramGb =
                int.Parse(
                    SelectedRam.Replace(" Go", ""));

            await _minecraftService.LaunchAsync(
                ramGb);
        }
        catch (Exception ex)
        {
            Status =
                $"Erreur : {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}