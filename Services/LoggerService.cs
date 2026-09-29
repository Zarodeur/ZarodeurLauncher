using System;
using System.IO;
using System.Text;

namespace ZarodeurLauncher.Services;

public class LoggerService
{
    private readonly string _logsPath;
    private readonly object _lock = new();

    public LoggerService()
    {
        var appData =
            Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData);

        var launcherPath =
            Path.Combine(
                appData,
                "ZarodeurLauncher");

        _logsPath =
            Path.Combine(
                launcherPath,
                "Logs");

        Directory.CreateDirectory(_logsPath);
    }

    // ============================================================
    // LOGS
    // ============================================================

    public void Info(string message)
    {
        Write("INFO", message);
    }

    public void Warning(string message)
    {
        Write("WARNING", message);
    }

    public void Error(string message)
    {
        Write("ERROR", message);
    }

    // ============================================================
    // ÉCRITURE
    // ============================================================

    private void Write(
        string level,
        string message)
    {
        var filePath =
            Path.Combine(
                _logsPath,
                $"{DateTime.Now:yyyy-MM-dd}.log");

        var line =
            $"[{DateTime.Now:HH:mm:ss}] [{level}] {message}";

        lock (_lock)
        {
            File.AppendAllText(
                filePath,
                line + Environment.NewLine,
                Encoding.UTF8);
        }
    }

    // ============================================================
    // DOSSIER DES LOGS
    // ============================================================

    public string GetLogsPath()
    {
        return _logsPath;
    }

    // ============================================================
    // LECTURE DES LOGS DU JOUR
    // ============================================================

    public string GetTodayLogs()
    {
        var filePath =
            Path.Combine(
                _logsPath,
                $"{DateTime.Now:yyyy-MM-dd}.log");

        if (!File.Exists(filePath))
        {
            return "Aucun log disponible.";
        }

        lock (_lock)
        {
            return File.ReadAllText(
                filePath,
                Encoding.UTF8);
        }
    }

    // ============================================================
    // SUPPRESSION DES LOGS DU JOUR
    // ============================================================

    public void ClearTodayLogs()
    {
        var filePath =
            Path.Combine(
                _logsPath,
                $"{DateTime.Now:yyyy-MM-dd}.log");

        lock (_lock)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}