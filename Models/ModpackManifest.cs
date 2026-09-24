using System.Collections.Generic;

namespace ZarodeurLauncher.Models;

public class ModpackManifest
{
    public string Name { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public MinecraftInfo Minecraft { get; set; } = new();

    public List<ModInfo> Mods { get; set; } = new();
}

public class MinecraftInfo
{
    public string Version { get; set; } = string.Empty;

    public string Loader { get; set; } = string.Empty;

    public string LoaderVersion { get; set; } = string.Empty;
}

public class ModInfo
{
    public string Id { get; set; } = string.Empty;

    public string File { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public string Sha256 { get; set; } = string.Empty;
}