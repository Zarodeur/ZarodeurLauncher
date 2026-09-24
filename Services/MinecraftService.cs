using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.Installer.Forge;
using CmlLib.Core.ProcessBuilder;

namespace ZarodeurLauncher.Services;

public class MinecraftService
{
    private readonly MinecraftPathService _pathService;

    private const string MinecraftVersion = "1.20.1";
    private const string ForgeVersion = "47.4.0";

    private const string JavaPath =
        @"C:\Program Files\Eclipse Adoptium\jdk-17.0.20.101-hotspot\bin\java.exe";

    public MinecraftService(MinecraftPathService pathService)
    {
        _pathService = pathService;
    }

    public async Task LaunchAsync()
    {
        var path = new MinecraftPath(
            _pathService.MinecraftPath);

        var launcher = new MinecraftLauncher(path);

        // Installation de Minecraft vanilla
        await launcher.InstallAsync(
            MinecraftVersion);

        // Installation de Forge
        var forgeInstaller = new ForgeInstaller(launcher);

        await forgeInstaller.Install(
            MinecraftVersion,
            ForgeVersion,
            new ForgeInstallOptions
            {
                JavaPath = JavaPath,
                SkipIfAlreadyInstalled = true
            });

        // Récupération du profil Forge
        var forgeVersionName =
            $"{MinecraftVersion}-forge-{ForgeVersion}";

        var forgeVersion =
            await launcher.GetVersionAsync(
                forgeVersionName);

        // Récupération des fichiers nécessaires à Forge
        var files =
            await launcher.ExtractFiles(
                forgeVersion);

        // Installation des bibliothèques Forge manquantes
        await launcher.GameInstaller.Install(
            files,
            null,
            null,
            default);

        // Session hors ligne
        var session =
            MSession.CreateOfflineSession("Zarodeur");

        // Construction du processus Minecraft
        var process =
            launcher.BuildProcess(
                forgeVersion,
                new MLaunchOption
                {
                    Session = session,
                    JavaPath = JavaPath
                });

        // Lancement
        process.Start();
    }
}