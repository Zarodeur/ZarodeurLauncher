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
    private readonly LoggerService _logger;

    // URL du manifest GitHub
    private const string ManifestUrl =
        "https://raw.githubusercontent.com/Zarodeur/ZarodeurModpack/main/manifest.json";

    public ModpackService(
        MinecraftPathService pathService)
    {
        _httpClient = new HttpClient();

        // GitHub peut refuser les requêtes sans User-Agent
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            "ZarodeurLauncher/1.0");

        _pathService = pathService;

        // Initialisation du système de logs
        _logger = new LoggerService();
    }

    // ============================================================
    // RÉCUPÉRATION DU MANIFEST
    // ============================================================

    public async Task<ModpackManifest> GetManifestAsync()
    {
        // Ajout d'un timestamp pour éviter qu'un ancien manifest
        // soit récupéré depuis un cache
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

    // ============================================================
    // SYNCHRONISATION DES MODS
    // ============================================================

    public async Task SynchronizeModsAsync(
        ModpackManifest manifest,
        Action<string, int, int, long, long>? progressCallback = null)
    {
        // Log du début de la synchronisation
        _logger.Info(
            $"Synchronisation du modpack v{manifest.Version} : " +
            $"{manifest.Mods.Count} mod(s) attendu(s)");

        // Liste des fichiers qui doivent être présents
        // d'après le manifest
        var expectedFiles =
            new HashSet<string>(
                manifest.Mods.Select(mod => mod.File),
                StringComparer.OrdinalIgnoreCase);

        // ========================================================
        // SUPPRESSION DES ANCIENS MODS
        // ========================================================

        // Récupération des fichiers .jar actuellement présents
        var localMods =
            Directory.GetFiles(
                _pathService.ModsPath,
                "*.jar");

        // Parcours de chaque mod local
        foreach (var localMod in localMods)
        {
            // Récupération uniquement du nom du fichier
            var fileName =
                Path.GetFileName(localMod);

            // Si le mod n'est plus présent dans le manifest,
            // il est devenu obsolète
            if (!expectedFiles.Contains(fileName))
            {
                _logger.Info(
                    $"Suppression du mod obsolète : {fileName}");

                File.Delete(localMod);
            }
        }

        // ========================================================
        // TÉLÉCHARGEMENT / VÉRIFICATION DES MODS
        // ========================================================

        // Parcours de tous les mods du manifest
        for (int i = 0; i < manifest.Mods.Count; i++)
        {
            var mod = manifest.Mods[i];

            await DownloadModAsync(
                mod,
                i + 1,
                manifest.Mods.Count,
                progressCallback);
        }

        // Fin de la synchronisation
        _logger.Info(
            $"Synchronisation du modpack v{manifest.Version} terminée");
    }

    // ============================================================
    // TÉLÉCHARGEMENT D'UN MOD
    // ============================================================

    private async Task<bool> DownloadModAsync(
        ModInfo mod,
        int currentMod,
        int totalMods,
        Action<string, int, int, long, long>? progressCallback = null)
    {
        // Chemin complet du mod sur le PC
        var destination =
            Path.Combine(
                _pathService.ModsPath,
                mod.File);

        // ========================================================
        // VÉRIFICATION DU MOD EXISTANT
        // ========================================================

        // Si le fichier existe déjà,
        // on vérifie son SHA-256
        if (File.Exists(destination))
        {
            var existingHash =
                await ComputeSha256Async(destination);

            // Le fichier est déjà correct
            if (string.Equals(
                existingHash,
                mod.Sha256,
                StringComparison.OrdinalIgnoreCase))
            {
                _logger.Info(
                    $"Mod déjà à jour : {mod.File}");

                // Indique à l'interface que ce mod est déjà à jour
                progressCallback?.Invoke(
                    mod.File,
                    currentMod,
                    totalMods,
                    1,
                    1);

                return false;
            }

            // Le fichier existe mais son SHA-256 est différent
            _logger.Warning(
                $"SHA-256 différent pour {mod.File}, téléchargement nécessaire");
        }

        // Fichier temporaire utilisé pendant le téléchargement
        var temporaryFile =
            destination + ".download";

        // Suppression d'un ancien téléchargement temporaire
        if (File.Exists(temporaryFile))
        {
            File.Delete(temporaryFile);
        }

        try
        {
            // ====================================================
            // TÉLÉCHARGEMENT
            // ====================================================

            _logger.Info(
                $"Téléchargement du mod : {mod.File}");

            using var response =
                await _httpClient.GetAsync(
                    mod.Url,
                    HttpCompletionOption.ResponseHeadersRead);

            response.EnsureSuccessStatusCode();

            // Taille totale du fichier
            var totalBytes =
                response.Content.Headers.ContentLength ?? -1;

            await using (var input =
                await response.Content.ReadAsStreamAsync())

            await using (var output =
                File.Create(temporaryFile))
            {
                // Buffer de téléchargement
                var buffer =
                    new byte[81920];

                long totalRead = 0;

                int bytesRead;

                // Téléchargement morceau par morceau
                while ((bytesRead =
                    await input.ReadAsync(buffer)) > 0)
                {
                    // Écriture des données sur le disque
                    await output.WriteAsync(
                        buffer.AsMemory(
                            0,
                            bytesRead));

                    totalRead += bytesRead;

                    // Mise à jour de la progression
                    progressCallback?.Invoke(
                        mod.File,
                        currentMod,
                        totalMods,
                        totalRead,
                        totalBytes);
                }

                await output.FlushAsync();
            }

            // ====================================================
            // VÉRIFICATION SHA-256
            // ====================================================

            _logger.Info(
                $"Vérification SHA-256 : {mod.File}");

            var downloadedHash =
                await ComputeSha256Async(
                    temporaryFile);

            // Vérification de l'intégrité du fichier
            if (!string.Equals(
                downloadedHash,
                mod.Sha256,
                StringComparison.OrdinalIgnoreCase))
            {
                _logger.Error(
                    $"SHA-256 invalide pour {mod.File}");

                throw new InvalidOperationException(
                    $"Le SHA-256 de {mod.File} ne correspond pas au manifest.");
            }

            _logger.Info(
                $"SHA-256 valide : {mod.File}");

            // ====================================================
            // INSTALLATION DU MOD
            // ====================================================

            // Si une ancienne version existe,
            // on la supprime
            if (File.Exists(destination))
            {
                File.Delete(destination);
            }

            // Le fichier temporaire devient le vrai fichier .jar
            File.Move(
                temporaryFile,
                destination);

            _logger.Info(
                $"Mod installé : {mod.File}");

            return true;
        }
        catch (Exception ex)
        {
            // ====================================================
            // GESTION DES ERREURS
            // ====================================================

            _logger.Error(
                $"Erreur avec le mod {mod.File} : {ex.Message}");

            // Suppression du fichier temporaire
            if (File.Exists(temporaryFile))
            {
                File.Delete(temporaryFile);
            }

            // On transmet l'erreur au launcher
            throw;
        }
    }

    // ============================================================
    // CALCUL DU SHA-256
    // ============================================================

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

    // ============================================================
    // RÉCUPÉRATION DE LA VERSION INSTALLÉE
    // ============================================================

    public string GetInstalledVersion()
    {
        // Si aucun fichier de version n'existe,
        // le modpack n'a jamais été installé
        if (!File.Exists(_pathService.ModpackVersionFile))
        {
            return string.Empty;
        }

        return File.ReadAllText(
            _pathService.ModpackVersionFile).Trim();
    }

    // ============================================================
    // SAUVEGARDE DE LA VERSION INSTALLÉE
    // ============================================================

    public void SaveInstalledVersion(string version)
    {
        File.WriteAllText(
            _pathService.ModpackVersionFile,
            version);
    }
}