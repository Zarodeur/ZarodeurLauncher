using System;
using System.IO;

namespace ZarodeurLauncher.Services;

public class MinecraftPathService
{
    public string MinecraftPath { get; }

    public string ModsPath { get; }

    public MinecraftPathService()
    {
        var appData = Environment.GetFolderPath(
            Environment.SpecialFolder.ApplicationData);

        MinecraftPath = Path.Combine(
            appData,
            "ZarodeurLauncher",
            "Minecraft");

        ModsPath = Path.Combine(
            MinecraftPath,
            "mods");

        Directory.CreateDirectory(MinecraftPath);
        Directory.CreateDirectory(ModsPath);
    }
}