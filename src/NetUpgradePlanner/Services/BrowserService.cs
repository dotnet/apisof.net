using System;
using System.Diagnostics;

namespace NetUpgradePlanner.Services;

internal static class BrowserService
{
    public static bool NavigateTo(Uri uri)
    {
        var isHttps = string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase);
        if (isHttps)
        {
            var startupInfo = new ProcessStartInfo
            {
                FileName = uri.ToString(),
                UseShellExecute = true
            };
            Process.Start(startupInfo);
            return true;
        }

        return false;
    }
}
