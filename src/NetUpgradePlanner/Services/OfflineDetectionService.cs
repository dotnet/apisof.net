using System;
using System.IO;

namespace NetUpgradePlanner.Services;

internal sealed class OfflineDetectionService
{
    public OfflineDetectionService()
    {
        var appDirectory = Path.GetDirectoryName(Environment.ProcessPath) ?? string.Empty;
        var offlineFile = Path.Join(appDirectory, "Offline.txt");
        IsOfflineInstallation = File.Exists(offlineFile);
    }
    
    public bool IsOfflineInstallation { get; }
}