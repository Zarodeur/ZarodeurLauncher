using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using ZarodeurLauncher.Models;

namespace ZarodeurLauncher.Services;

public class ModpackService
{
    private readonly HttpClient _httpClient;
    private readonly MinecraftPathService _pathService;

    private const string ManifestUrl =
        "https://raw.githubusercontent.com/Zarodeur/ZarodeurModpack/main/manifest.json";

    public ModpackService(
        MinecraftPathService pathService)
    {
        _httpClient = new HttpClient();

        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            "ZarodeurLauncher/1.0");

        _pathService = pathService;
    }

    public async Task<ModpackManifest> GetManifestAsync()
    {
        var json =
            await _httpClient.GetStringAsync(ManifestUrl);

        var manifest =
            JsonSerializer.Deserialize<ModpackManifest>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

        return manifest
            ?? throw new InvalidOperationException(
                "Impossible de lire le manifest.");
    }

    public async Task<bool> DownloadModAsync(ModInfo mod)
    {
        var destination =
            Path.Combine(
                _pathService.ModsPath,
                mod.File);

        // Le fichier existe déjà : on vérifie son SHA-256
        if (File.Exists(destination))
        {
            var existingHash =
                await ComputeSha256Async(destination);

            if (string.Equals(
                existingHash,
                mod.Sha256,
                StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        // Le fichier est absent ou différent : téléchargement
        var data =
            await _httpClient.GetByteArrayAsync(mod.Url);

        await File.WriteAllBytesAsync(
            destination,
            data);

        // Vérification après téléchargement
        var downloadedHash =
            await ComputeSha256Async(destination);

        if (!string.Equals(
            downloadedHash,
            mod.Sha256,
            StringComparison.OrdinalIgnoreCase))
        {
            File.Delete(destination);

            throw new InvalidOperationException(
                $"Le SHA-256 de {mod.File} ne correspond pas au manifest.");
        }

        return true;
    }
    private static async Task<string> ComputeSha256Async(
    string filePath)
    {
        using var sha256 =
            System.Security.Cryptography.SHA256.Create();

        await using var stream =
            File.OpenRead(filePath);

        var hash =
            await sha256.ComputeHashAsync(stream);

        return Convert.ToHexString(hash);
    }
} 