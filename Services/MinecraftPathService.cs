using System;
using System.IO;

namespace ZarodeurLauncher.Services;

public class MinecraftPathService
{
    public string MinecraftPath { get; }

    public string ModsPath { get; }

    public string ModpackVersionFile { get; }

    public MinecraftPathService()
    {
        var appData = Environment.GetFolderPath(
            Environment.SpecialFolder.ApplicationData);

        var launcherPath = Path.Combine(
            appData,
            "ZarodeurLauncher");

        MinecraftPath = Path.Combine(
            launcherPath,
            "Minecraft");

        ModsPath = Path.Combine(
            MinecraftPath,
            "mods");

        ModpackVersionFile = Path.Combine(
            launcherPath,
            "modpack-version.txt");

        Directory.CreateDirectory(
            launcherPath);

        Directory.CreateDirectory(
            MinecraftPath);

        Directory.CreateDirectory(
            ModsPath);
    }
}