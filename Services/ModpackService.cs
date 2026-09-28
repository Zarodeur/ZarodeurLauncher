using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        var url =
            $"{ManifestUrl}?t={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

        var json =
            await _httpClient.GetStringAsync(url);

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

    public async Task SynchronizeModsAsync(
        ModpackManifest manifest,
        Action<string, int, int, long, long>? progressCallback = null)
    {
        var expectedFiles =
            new HashSet<string>(
                manifest.Mods.Select(mod => mod.File),
                StringComparer.OrdinalIgnoreCase);

        // Suppression des mods qui ne sont plus dans le manifest
        var localMods =
            Directory.GetFiles(
                _pathService.ModsPath,
                "*.jar");

        foreach (var localMod in localMods)
        {
            var fileName =
                Path.GetFileName(localMod);

            if (!expectedFiles.Contains(fileName))
            {
                File.Delete(localMod);
            }
        }

        // Téléchargement / vérification des mods
        for (int i = 0; i < manifest.Mods.Count; i++)
        {
            var mod = manifest.Mods[i];

            await DownloadModAsync(
                mod,
                i + 1,
                manifest.Mods.Count,
                progressCallback);
        }
    }

    private async Task<bool> DownloadModAsync(
        ModInfo mod,
        int currentMod,
        int totalMods,
        Action<string, int, int, long, long>? progressCallback = null)
    {
        var destination =
            Path.Combine(
                _pathService.ModsPath,
                mod.File);

        // Le fichier existe déjà et son SHA-256 est correct
        if (File.Exists(destination))
        {
            var existingHash =
                await ComputeSha256Async(destination);

            if (string.Equals(
                existingHash,
                mod.Sha256,
                StringComparison.OrdinalIgnoreCase))
            {
                progressCallback?.Invoke(
                    mod.File,
                    currentMod,
                    totalMods,
                    1,
                    1);

                return false;
            }
        }

        var temporaryFile =
            destination + ".download";

        // Nettoyage d'un ancien téléchargement temporaire
        if (File.Exists(temporaryFile))
        {
            File.Delete(temporaryFile);
        }

        try
        {
            using var response =
                await _httpClient.GetAsync(
                    mod.Url,
                    HttpCompletionOption.ResponseHeadersRead);

            response.EnsureSuccessStatusCode();

            var totalBytes =
                response.Content.Headers.ContentLength ?? -1;

            await using (var input =
                await response.Content.ReadAsStreamAsync())
            await using (var output =
                File.Create(temporaryFile))
            {
                var buffer =
                    new byte[81920];

                long totalRead = 0;

                int bytesRead;

                while ((bytesRead =
                    await input.ReadAsync(buffer)) > 0)
                {
                    await output.WriteAsync(
                        buffer.AsMemory(
                            0,
                            bytesRead));

                    totalRead += bytesRead;

                    progressCallback?.Invoke(
                        mod.File,
                        currentMod,
                        totalMods,
                        totalRead,
                        totalBytes);
                }

                await output.FlushAsync();
            }

            // Vérification SHA-256
            var downloadedHash =
                await ComputeSha256Async(
                    temporaryFile);

            if (!string.Equals(
                downloadedHash,
                mod.Sha256,
                StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Le SHA-256 de {mod.File} ne correspond pas au manifest.");
            }

            // Suppression de l'ancien fichier
            if (File.Exists(destination))
            {
                File.Delete(destination);
            }

            // Déplacement du fichier validé
            File.Move(
                temporaryFile,
                destination);

            return true;
        }
        catch
        {
            if (File.Exists(temporaryFile))
            {
                File.Delete(temporaryFile);
            }

            throw;
        }
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
    public string GetInstalledVersion()
    {
        if (!File.Exists(_pathService.ModpackVersionFile))
        {
            return string.Empty;
        }

        return File.ReadAllText(
            _pathService.ModpackVersionFile).Trim();
    }

    public void SaveInstalledVersion(string version)
    {
        File.WriteAllText(
            _pathService.ModpackVersionFile,
            version);
    }
}