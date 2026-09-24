using System;
using System.IO;

namespace ZarodeurLauncher.Services;

public class MinecraftPathService
{
    public string MinecraftPath { get; }

    public MinecraftPathService()
    {
        var appData = Environment.GetFolderPath(
            Environment.SpecialFolder.ApplicationData);

        MinecraftPath = Path.Combine(
            appData,
            "ZarodeurLauncher",
            "Minecraft");

        Directory.CreateDirectory(MinecraftPath);
    }
}