using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ZarodeurLauncher.Services;
using Avalonia.Controls;
using ZarodeurLauncher.Views;
using CmlLib.Core.Auth;

namespace ZarodeurLauncher.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly MinecraftPathService _minecraftPathService;
    private readonly MinecraftService _minecraftService;
    private readonly ModpackService _modpackService;
    private readonly LauncherSettingsService _settingsService;
    private readonly LoggerService _logger;
    private readonly MicrosoftAuthService _microsoftAuthService;
    private MSession? _microsoftSession;

    [ObservableProperty]
    private string minecraftUsername = "Non connecté";

    [ObservableProperty]
    private string _logs = "Aucun log disponible.";

    [ObservableProperty]
    private bool _showSettings;

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
        _minecraftPathService =
            new MinecraftPathService();

        _settingsService =
            new LauncherSettingsService();

        _selectedRam =
            $"{_settingsService.GetRam()} Go";

        _logger =
            new LoggerService();

        _logger.Info(
            "Launcher démarré");

        _microsoftAuthService =
            new MicrosoftAuthService();

        _minecraftService =
            new MinecraftService(
                _minecraftPathService);

        _modpackService =
            new ModpackService(
                _minecraftPathService);
    }

    // ============================================================
    // RAM
    // ============================================================

    partial void OnSelectedRamChanged(
        string value)
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

    // ============================================================
    // NAVIGATION
    // ============================================================

    [RelayCommand]
    private void OpenSettings()
    {
        ShowSettings = true;
    }

    [RelayCommand]
    private void CloseSettings()
    {
        ShowSettings = false;
    }

    // ============================================================
    // LOGS
    // ============================================================

    [RelayCommand]
    private void OpenLogs()
    {
        Logs =
            _logger.GetTodayLogs();
    }

    [RelayCommand]
    private void RefreshLogs()
    {
        Logs =
            _logger.GetTodayLogs();
    }

    [RelayCommand]
    private void ClearLogs()
    {
        if (App.Current?.ApplicationLifetime
            is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (desktop.MainWindow
                is MainWindow mainWindow)
            {
                mainWindow.ShowClearLogsConfirmation();
            }
        }
    }

    public void ConfirmClearLogs()
    {
        _logger.ClearTodayLogs();

        Logs =
            "Logs supprimés.";
    }

    // ============================================================
    // AUTHENTIFICATION MICROSOFT
    // ============================================================

    public async Task AuthenticateMicrosoftAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;

        try
        {
            Status =
                "Connexion à Microsoft...";

            _logger.Info(
                "Début de l'authentification Microsoft");

            _microsoftSession =
                await _microsoftAuthService.LoginAsync();

            MinecraftUsername =
                _microsoftSession.Username ?? "Compte Microsoft";

            _logger.Info(
                $"Compte Microsoft connecté : {_microsoftSession.Username}");

            _logger.Info(
                $"UUID Minecraft : {_microsoftSession.UUID}");

            Status =
                $"Connecté : {MinecraftUsername}";
        }
        catch (Exception ex)
        {
            _logger.Error(
                $"Erreur d'authentification Microsoft : {ex.Message}");

            MinecraftUsername =
                "Non connecté";

            Status =
                "Connexion Microsoft impossible.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ============================================================
    // VÉRIFICATION DU MODPACK
    // ============================================================

    public async Task CheckModpackAsync()
    {
        try
        {
            _logger.Info(
                "Vérification du modpack");

            ModpackStatus =
                "Vérification...";

            var manifest =
                await _modpackService.GetManifestAsync();

            _logger.Info(
                $"Manifest v{manifest.Version} récupéré");

            var installedVersion =
                _modpackService.GetInstalledVersion();

            ModpackVersion =
                $"Version disponible : {manifest.Version}";

            if (string.IsNullOrEmpty(
                installedVersion))
            {
                _logger.Info(
                    "Aucune version du modpack installée");

                ModpackStatus =
                    "Installation requise";

                PlayButtonText =
                    "INSTALLER";
            }
            else if (
                installedVersion != manifest.Version)
            {
                _logger.Info(
                    $"Mise à jour disponible : " +
                    $"{installedVersion} → {manifest.Version}");

                ModpackStatus =
                    $"⚠ Mise à jour disponible • " +
                    $"{installedVersion} → {manifest.Version}";

                PlayButtonText =
                    "METTRE À JOUR";
            }
            else
            {
                _logger.Info(
                    $"Modpack à jour : v{manifest.Version}");

                ModpackStatus =
                    "✓ Modpack à jour";

                PlayButtonText =
                    "JOUER";
            }
        }
        catch (Exception ex)
        {
            _logger.Error(
                $"Erreur lors de la vérification du modpack : " +
                $"{ex.Message}");

            ModpackStatus =
                $"Impossible de vérifier le modpack : " +
                $"{ex.Message}";
        }
    }

    // ============================================================
    // PRÉPARATION ET LANCEMENT DE MINECRAFT
    // ============================================================

    [RelayCommand]
    private async Task PrepareMinecraft()
    {
        if (IsBusy)
            return;

        IsBusy = true;

        try
        {
            Status =
                "Récupération du modpack...";

            var manifest =
                await _modpackService.GetManifestAsync();

            var installedVersion =
                _modpackService.GetInstalledVersion();

            ModpackVersion =
                $"Version disponible : {manifest.Version}";

            if (string.IsNullOrEmpty(
                installedVersion))
            {
                Status =
                    $"Première installation du modpack\n" +
                    $"Version disponible : {manifest.Version}";
            }
            else if (
                installedVersion != manifest.Version)
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

            Status =
                "Synchronisation des mods...";

            Progress = 0;

            await _modpackService.SynchronizeModsAsync(
                manifest,
                (fileName,
                currentMod,
                totalMods,
                currentBytes,
                totalBytes) =>
                {
                    if (currentBytes == 1 &&
                        totalBytes == 1)
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
                            currentBytes /
                            1024.0 /
                            1024.0;

                        var totalMb =
                            totalBytes /
                            1024.0 /
                            1024.0;

                        Status =
                            $"Téléchargement de {fileName}\n" +
                            $"Mod {currentMod}/{totalMods}\n" +
                            $"{currentMb:0.0} / " +
                            $"{totalMb:0.0} Mo";
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

            PlayButtonText =
                "JOUER";

            ModpackVersion =
                $"Version installée : {manifest.Version}";

            ModpackStatus =
                "✓ Modpack à jour";

            Status =
                $"Modpack prêt ! Version {manifest.Version}";

            // ========================================================
            // SESSION MICROSOFT
            // ========================================================

            if (_microsoftSession == null)
            {
                Status =
                    "Connexion à Microsoft...";

                _logger.Info(
                    "Aucune session Microsoft disponible, nouvelle connexion");

                _microsoftSession =
                    await _microsoftAuthService.LoginAsync();

                MinecraftUsername =
                    _microsoftSession.Username ?? "Compte Microsoft";

                _logger.Info(
                    $"Compte Microsoft connecté : {_microsoftSession.Username}");

                _logger.Info(
                    $"UUID Minecraft : {_microsoftSession.UUID}");
            }
            else
            {
                _logger.Info(
                    $"Session Microsoft déjà disponible : {_microsoftSession.Username}");
            }

            // ========================================================
            // LANCEMENT DE MINECRAFT
            // ========================================================

            Status =
                "Lancement de Minecraft...";

            var ramGb =
                int.Parse(
                    SelectedRam.Replace(
                        " Go",
                        ""));

            await _minecraftService.LaunchAsync(
                ramGb,
                _microsoftSession);
        }
        catch (Exception ex)
        {
            _logger.Error(
                $"Erreur pendant la préparation de Minecraft : {ex.Message}");

            Status =
                $"Erreur : {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}