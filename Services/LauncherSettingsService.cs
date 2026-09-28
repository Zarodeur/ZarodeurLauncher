using System;
using System.IO;

namespace ZarodeurLauncher.Services;

public class LauncherSettingsService
{
    private readonly string _settingsFile;

    public LauncherSettingsService()
    {
        var appData =
            Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData);

        var launcherPath =
            Path.Combine(
                appData,
                "ZarodeurLauncher");

        Directory.CreateDirectory(launcherPath);

        _settingsFile =
            Path.Combine(
                launcherPath,
                "settings.txt");
    }

    public int GetRam()
    {
        if (!File.Exists(_settingsFile))
        {
            return 6;
        }

        var content =
            File.ReadAllText(_settingsFile).Trim();

        if (int.TryParse(content, out var ram))
        {
            if (ram == 4 ||
                ram == 6 ||
                ram == 8 ||
                ram == 10 ||
                ram == 12 ||
                ram == 14 ||
                ram == 16)
            {
                return ram;
            }
        }

        return 6;
    }

    public void SaveRam(int ram)
    {
        if (ram != 4 &&
            ram != 6 &&
            ram != 8 &&
            ram != 10 &&
            ram != 12 &&
            ram != 14 &&
            ram != 16)
        {
            throw new ArgumentException(
                "La quantité de RAM sélectionnée est invalide.");
        }

        File.WriteAllText(
            _settingsFile,
            ram.ToString());
    }
}