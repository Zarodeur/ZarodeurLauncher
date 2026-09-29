using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.Installer.Forge;
using CmlLib.Core.ProcessBuilder;

namespace ZarodeurLauncher.Services;

public class MinecraftService
{
    private readonly MinecraftPathService _pathService;
    private readonly LoggerService _logger;

    // ============================================================
    // CONFIGURATION MINECRAFT
    // ============================================================

    // Version de Minecraft utilisée par le modpack
    private const string MinecraftVersion = "1.20.1";

    // Version de Forge utilisée par le modpack
    private const string ForgeVersion = "47.4.0";

    // Chemin vers Java 17
    private const string JavaPath =
        @"C:\Program Files\Eclipse Adoptium\jdk-17.0.20.101-hotspot\bin\javaw.exe";

    public MinecraftService(
        MinecraftPathService pathService)
    {
        _pathService = pathService;

        // Initialisation du système de logs
        _logger = new LoggerService();
    }

    // ============================================================
    // LANCEMENT DE MINECRAFT
    // ============================================================

    public async Task LaunchAsync(int ramGb)
    {
        try
        {
            // ----------------------------------------------------
            // INFORMATIONS DE DÉMARRAGE
            // ----------------------------------------------------

            _logger.Info(
                "Préparation du lancement de Minecraft");

            _logger.Info(
                $"Minecraft {MinecraftVersion}");

            _logger.Info(
                $"Forge {ForgeVersion}");

            _logger.Info(
                $"RAM sélectionnée : {ramGb} Go");

            // ----------------------------------------------------
            // CHEMIN MINECRAFT
            // ----------------------------------------------------

            _logger.Info(
                $"Dossier Minecraft : {_pathService.MinecraftPath}");

            var path =
                new MinecraftPath(
                    _pathService.MinecraftPath);

            // Création du launcher CmlLib
            var launcher =
                new MinecraftLauncher(path);

            // ----------------------------------------------------
            // INSTALLATION DE MINECRAFT VANILLA
            // ----------------------------------------------------

            _logger.Info(
                $"Vérification de Minecraft {MinecraftVersion}");

            await launcher.InstallAsync(
                MinecraftVersion);

            _logger.Info(
                "Minecraft vanilla prêt");

            // ----------------------------------------------------
            // INSTALLATION DE FORGE
            // ----------------------------------------------------

            _logger.Info(
                $"Vérification de Forge {ForgeVersion}");

            var forgeInstaller =
                new ForgeInstaller(launcher);

            await forgeInstaller.Install(
                MinecraftVersion,
                ForgeVersion,
                new ForgeInstallOptions
                {
                    JavaPath = JavaPath,
                    SkipIfAlreadyInstalled = true
                });

            _logger.Info(
                $"Forge {ForgeVersion} prêt");

            // ----------------------------------------------------
            // RÉCUPÉRATION DU PROFIL FORGE
            // ----------------------------------------------------

            var forgeVersionName =
                $"{MinecraftVersion}-forge-{ForgeVersion}";

            _logger.Info(
                $"Chargement du profil Forge : {forgeVersionName}");

            var forgeVersion =
                await launcher.GetVersionAsync(
                    forgeVersionName);

            // ----------------------------------------------------
            // RÉCUPÉRATION DES FICHIERS FORGE
            // ----------------------------------------------------

            _logger.Info(
                "Vérification des bibliothèques Forge");

            var files =
                await launcher.ExtractFiles(
                    forgeVersion);

            // Installation des bibliothèques manquantes
            await launcher.GameInstaller.Install(
                files,
                null,
                null,
                default);

            _logger.Info(
                "Bibliothèques Forge prêtes");

            // ----------------------------------------------------
            // SESSION MINECRAFT
            // ----------------------------------------------------

            // Pour le moment nous utilisons une session hors ligne
            var session =
                MSession.CreateOfflineSession(
                    "Zarodeur");

            _logger.Info(
                "Session Minecraft hors ligne créée");

            // ----------------------------------------------------
            // CONSTRUCTION DU PROCESSUS
            // ----------------------------------------------------

            _logger.Info(
                "Construction du processus Minecraft");

            var process =
                launcher.BuildProcess(
                    forgeVersion,
                    new MLaunchOption
                    {
                        Session = session,

                        JavaPath = JavaPath,

                        // RAM minimale
                        MinimumRamMb = 2048,

                        // RAM maximale choisie dans le launcher
                        MaximumRamMb = ramGb * 1024
                    });

            // ----------------------------------------------------
            // CONFIGURATION DE LA FENÊTRE JAVA
            // ----------------------------------------------------

            // javaw.exe évite normalement l'apparition
            // d'une fenêtre console.
            //
            // Ces paramètres permettent également de masquer
            // la console du processus.
            process.StartInfo.UseShellExecute = false;

            process.StartInfo.CreateNoWindow = true;

            process.StartInfo.WindowStyle =
                ProcessWindowStyle.Hidden;

            // ----------------------------------------------------
            // LANCEMENT
            // ----------------------------------------------------

            _logger.Info(
                "Lancement de Minecraft");

            process.Start();

            _logger.Info(
                "Minecraft lancé avec succès");
        }
        catch (Exception ex)
        {
            // ----------------------------------------------------
            // ERREUR
            // ----------------------------------------------------

            _logger.Error(
                $"Impossible de lancer Minecraft : {ex.Message}");

            throw;
        }
    }
}