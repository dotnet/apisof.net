using System.Runtime.InteropServices;

using Microsoft.Win32;

namespace NetUpgradePlanner;

internal static class FileExtensionManager
{
    private const uint SHCNE_ASSOCCHANGED = 0x08000000;
    private const uint SHCNF_IDLIST = 0x0000;
    private const uint SHCNF_FLUSHNOWAIT = 0x2000;

    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(uint wEventId,
                                              uint uFlags,
                                              nuint dwItem1,
                                              nuint dwItem2);

    private static void NotifyWindows()
    {
        SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST | SHCNF_FLUSHNOWAIT, 0, 0);
    }

    public static void RegisterFileExtensions()
    {
        Registry
            .CurrentUser
            .CreateSubKey($"Software\\Classes\\.nupproj", writable: true)
            .SetValue(null, "NetUpgradePlanner.nupproj");

        Registry
            .CurrentUser
            .CreateSubKey($"Software\\Classes\\NetUpgradePlanner.nupproj", writable: true)
            .SetValue(null, $".NET Upgrade Planner Project File");

        Registry
            .CurrentUser
            .CreateSubKey($"Software\\Classes\\NetUpgradePlanner.nupproj\\shell\\open\\command", writable: true)
            .SetValue(null, $"{Environment.ProcessPath} %1");

        NotifyWindows();
    }

    public static void UnregisterFileExtensions()
    {
        Registry.CurrentUser.DeleteSubKeyTree($"Software\\Classes\\.nupproj");
        Registry.CurrentUser.DeleteSubKeyTree($"Software\\Classes\\NetUpgradePlanner.nupproj");

        NotifyWindows();
    }
}