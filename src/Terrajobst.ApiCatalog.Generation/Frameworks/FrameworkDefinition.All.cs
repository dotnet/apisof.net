using System.Text.Json;

namespace Terrajobst.ApiCatalog;

// NOTE: Virtually all of the information here is extracted from the .NET SDK
// using the DumpPacks tool; however for that to work, you need to have the
// workloads installed (for each major.minor of .NET separately).
//
// We do this because interrogating the SDK during catalog construction is too
// cumbersome, for starters you'd need the workloads installed, but also
// because there is no straight forward way to do this. Right now, DumpPacks
// just scans for specific patterns in the XML and the file structure and
// relies on manually making sense of it. While that's not ideal, it's good
// enough given that there aren't many parties that need to understand this
// and the information is basically static anyway.

public partial class FrameworkDefinition
{
    public static IReadOnlyList<FrameworkDefinition> All { get; } =
    [
        new FrameworkDefinition("netcoreapp3.0")
        {
            BuiltInPacks =
            [
                new PackReference("Microsoft.NETCore.App.Ref")
                {
                    Version = "3.0",
                    Platforms = [""],
                    Kind = PackKind.Framework
                },
                new PackReference("Microsoft.AspNetCore.App.Ref")
                {
                    Version = "3.0",
                    Platforms = [""],
                    Kind = PackKind.Framework
                },
                new PackReference("Microsoft.WindowsDesktop.App.Ref")
                {
                    Version = "3.0",
                    Platforms = ["windows"],
                    Kind = PackKind.Framework
                },
            ]
        },
        new FrameworkDefinition("netcoreapp3.1")
        {
            BuiltInPacks =
            [
                new PackReference("Microsoft.NETCore.App.Ref")
                {
                    Version = "3.1",
                    Platforms = [""],
                    Kind = PackKind.Framework
                },
                new PackReference("Microsoft.AspNetCore.App.Ref")
                {
                    Version = "3.1",
                    Platforms = [""],
                    Kind = PackKind.Framework
                },
                new PackReference("Microsoft.WindowsDesktop.App.Ref")
                {
                    Version = "3.1",
                    Platforms = ["windows"],
                    Kind = PackKind.Framework
                },
            ]
        },
        new FrameworkDefinition("net5.0")
        {
            SupportedPlatforms =
            [
                new FrameworkPlatformDefinition("windows")
                {
                    Versions = ["7.0", "8.0", "10.0.17763", "10.0.18362", "10.0.19041", "10.0.20348", "10.0.22000"]
                }
            ],
            BuiltInPacks =
            [
                new PackReference("Microsoft.NETCore.App.Ref")
                {
                    Version = "5.0",
                    Platforms = [""],
                    Kind = PackKind.Framework
                },
                new PackReference("Microsoft.AspNetCore.App.Ref")
                {
                    Version = "5.0",
                    Platforms = [""],
                    Kind = PackKind.Framework
                },
                new PackReference("Microsoft.WindowsDesktop.App.Ref")
                {
                    Version = "5.0",
                    Platforms = ["windows"],
                    Kind = PackKind.Framework
                },
                new PackReference("Microsoft.Windows.SDK.NET.Ref")
                {
                    Version = "10.0.17763",
                    Platforms = ["windows10.0.17763"],
                    Kind = PackKind.Framework
                },
                new PackReference("Microsoft.Windows.SDK.NET.Ref")
                {
                    Version = "10.0.18362",
                    Platforms = ["windows10.0.18362"],
                    Kind = PackKind.Framework
                },
                new PackReference("Microsoft.Windows.SDK.NET.Ref")
                {
                    Version = "10.0.19041",
                    Platforms = ["windows10.0.19041"],
                    Kind = PackKind.Framework
                },
                new PackReference("Microsoft.Windows.SDK.NET.Ref")
                {
                    Version = "10.0.20348",
                    Platforms = ["windows10.0.20348"],
                    Kind = PackKind.Framework
                },
                new PackReference("Microsoft.Windows.SDK.NET.Ref")
                {
                    Version = "10.0.22000",
                    Platforms = ["windows10.0.22000"],
                    Kind = PackKind.Framework
                }
            ]
        },
        new FrameworkDefinition("net6.0")
        {
            SupportedPlatforms =
            [
                new FrameworkPlatformDefinition("android")
                {
                    Versions = ["21.0", "22.0", "23.0", "24.0", "25.0", "26.0", "27.0", "28.0", "29.0", "30.0", "31.0", "32.0", "33.0"]
                },
                new FrameworkPlatformDefinition("ios")
                {
                    Versions = ["10.0", "10.1", "10.2", "10.3", "11.0", "11.1", "11.2", "11.3", "11.4", "12.0", "12.1", "12.2", "12.3", "12.4", "13.0", "13.1", "13.2", "13.3", "13.4", "13.5", "13.6", "14.0", "14.1", "14.2", "14.3", "14.4", "14.5", "15.0", "15.2", "15.4", "16.0", "16.1", "16.2", "16.4"]
                },
                new FrameworkPlatformDefinition("maccatalyst")
                {
                    Versions = ["13.1", "13.2", "13.3", "13.4", "13.5", "14.2", "14.3", "14.4", "14.5", "15.0", "15.2", "15.4", "16.1", "16.2", "16.4"]
                },
                new FrameworkPlatformDefinition("macos")
                {
                    Versions = ["10.14", "10.15", "10.16", "11.0", "11.1", "11.2", "11.3", "12.0", "12.1", "12.3", "13.0", "13.1", "13.3"]
                },
                new FrameworkPlatformDefinition("tvos")
                {
                    Versions = ["10.0", "10.1", "10.2", "11.0", "11.1", "11.2", "11.3", "11.4", "12.0", "12.1", "12.2", "12.3", "12.4", "13.0", "13.2", "13.3", "13.4", "14.0", "14.2", "14.3", "14.4", "14.5", "15.0", "15.2", "15.4", "16.0", "16.1", "16.4"]
                },
                new FrameworkPlatformDefinition("windows")
                {
                    Versions = ["7.0", "8.0", "10.0.17763", "10.0.18362", "10.0.19041", "10.0.20348", "10.0.22000", "10.0.22621"]
                }
            ],
            BuiltInPacks =
            [
                new PackReference("Microsoft.NETCore.App.Ref")
                {
                    Version = "6.0",
                    Platforms = [""],
                    Kind = PackKind.Framework
                },
                new PackReference("Microsoft.AspNetCore.App.Ref")
                {
                    Version = "6.0",
                    Platforms = [""],
                    Kind = PackKind.Framework
                },
                new PackReference("Microsoft.WindowsDesktop.App.Ref")
                {
                    Version = "6.0",
                    Platforms = ["windows"],
                    Kind = PackKind.Framework
                },
                new PackReference("Microsoft.Windows.SDK.NET.Ref")
                {
                    Version = "10.0.17763",
                    Platforms = ["windows10.0.17763"],
                    Kind = PackKind.Framework
                },
                new PackReference("Microsoft.Windows.SDK.NET.Ref")
                {
                    Version = "10.0.18362",
                    Platforms = ["windows10.0.18362"],
                    Kind = PackKind.Framework
                },
                new PackReference("Microsoft.Windows.SDK.NET.Ref")
                {
                    Version = "10.0.19041",
                    Platforms = ["windows10.0.19041"],
                    Kind = PackKind.Framework
                },
                new PackReference("Microsoft.Windows.SDK.NET.Ref")
                {
                    Version = "10.0.20348",
                    Platforms = ["windows10.0.20348"],
                    Kind = PackKind.Framework
                },
                new PackReference("Microsoft.Windows.SDK.NET.Ref")
                {
                    Version = "10.0.22000",
                    Platforms = ["windows10.0.22000"],
                    Kind = PackKind.Framework
                },
                new PackReference("Microsoft.Windows.SDK.NET.Ref")
                {
                    Version = "10.0.22621",
                    Platforms = ["windows10.0.22621"],
                    Kind = PackKind.Framework
                }
            ],
            WorkloadPacks =
            [
                new PackReference("Microsoft.Android.Ref.33")
                {
                    Version = "32.0.301",
                    Kind = PackKind.Framework,
                    Platforms = ["android"],
                    Workloads = ["android-33"]
                },
                new PackReference("Microsoft.AspNetCore.Components.WebView.Maui")
                {
                    Version = "6.0.312",
                    Kind = PackKind.Library,
                    Workloads = ["maui", "maui-android", "maui-desktop", "maui-ios", "maui-maccatalyst", "maui-mobile", "maui-tizen", "maui-windows"]
                },
                new PackReference("Microsoft.iOS.Ref")
                {
                    Version = "15.4.303",
                    Kind = PackKind.Framework,
                    Platforms = ["ios"],
                    Workloads = ["ios", "maui", "maui-ios", "maui-mobile"]
                },
                new PackReference("Microsoft.MacCatalyst.Ref")
                {
                    Version = "15.4.303",
                    Kind = PackKind.Framework,
                    Platforms = ["maccatalyst"],
                    Workloads = ["maccatalyst", "maui", "maui-desktop", "maui-maccatalyst"]
                },
                new PackReference("Microsoft.macOS.Ref")
                {
                    Version = "12.3.303",
                    Kind = PackKind.Framework,
                    Platforms = ["macos"],
                    Workloads = ["macos"]
                },
                new PackReference("Microsoft.Maui.Controls.Ref.android")
                {
                    Version = "6.0.312",
                    Kind = PackKind.Framework,
                    Platforms = ["android"],
                    Workloads = ["maui", "maui-android", "maui-mobile"]
                },
                new PackReference("Microsoft.Maui.Controls.Ref.any")
                {
                    Version = "6.0.312",
                    Kind = PackKind.Framework,
                    Platforms = [""],
                    Workloads = ["maui", "maui-android", "maui-desktop", "maui-ios", "maui-maccatalyst", "maui-mobile", "maui-tizen", "maui-windows"]
                },
                new PackReference("Microsoft.Maui.Controls.Ref.ios")
                {
                    Version = "6.0.312",
                    Kind = PackKind.Framework,
                    Platforms = ["ios"],
                    Workloads = ["maui", "maui-ios", "maui-mobile"]
                },
                new PackReference("Microsoft.Maui.Controls.Ref.maccatalyst")
                {
                    Version = "6.0.312",
                    Kind = PackKind.Framework,
                    Platforms = ["maccatalyst"],
                    Workloads = ["maui", "maui-maccatalyst", "maui-desktop"]
                },
                new PackReference("Microsoft.Maui.Controls.Ref.tizen")
                {
                    Version = "6.0.312",
                    Kind = PackKind.Framework,
                    Platforms = ["tizen"],
                    Workloads = ["maui", "maui-tizen", "maui-mobile"]
                },
                new PackReference("Microsoft.Maui.Controls.Ref.win")
                {
                    Version = "6.0.312",
                    Kind = PackKind.Framework,
                    Platforms = ["windows"],
                    Workloads = ["maui", "maui-desktop", "maui-windows"]
                },
                new PackReference("Microsoft.Maui.Core.Ref.android")
                {
                    Version = "6.0.312",
                    Kind = PackKind.Framework,
                    Platforms = ["android"],
                    Workloads = ["maui", "maui-android", "maui-mobile"]
                },
                new PackReference("Microsoft.Maui.Core.Ref.any")
                {
                    Version = "6.0.312",
                    Kind = PackKind.Framework,
                    Platforms = [""],
                    Workloads = ["maui", "maui-android", "maui-desktop", "maui-ios", "maui-maccatalyst", "maui-mobile", "maui-tizen", "maui-windows"]
                },
                new PackReference("Microsoft.Maui.Core.Ref.ios")
                {
                    Version = "6.0.312",
                    Kind = PackKind.Framework,
                    Platforms = ["ios"],
                    Workloads = ["maui", "maui-ios", "maui-mobile"]
                },
                new PackReference("Microsoft.Maui.Core.Ref.maccatalyst")
                {
                    Version = "6.0.312",
                    Kind = PackKind.Framework,
                    Platforms = ["maccatalyst"],
                    Workloads = ["maui", "maui-desktop", "maui-maccatalyst"]
                },
                new PackReference("Microsoft.Maui.Core.Ref.tizen")
                {
                    Version = "6.0.312",
                    Kind = PackKind.Framework,
                    Platforms = ["tizen"],
                    Workloads = ["maui", "maui-mobile", "maui-tizen"]
                },
                new PackReference("Microsoft.Maui.Core.Ref.win")
                {
                    Version = "6.0.312",
                    Kind = PackKind.Framework,
                    Platforms = ["windows"],
                    Workloads = ["maui-desktop", "maui", "maui-windows"]
                },
                new PackReference("Microsoft.Maui.Dependencies")
                {
                    Version = "6.0.312",
                    Kind = PackKind.Library,
                    Workloads = ["maui", "maui-android", "maui-desktop", "maui-ios", "maui-maccatalyst", "maui-mobile", "maui-tizen", "maui-windows"]
                },
                new PackReference("Microsoft.Maui.Essentials.Ref.android")
                {
                    Version = "6.0.312",
                    Kind = PackKind.Framework,
                    Platforms = ["android"],
                    Workloads = ["maui", "maui-android", "maui-mobile"]
                },
                new PackReference("Microsoft.Maui.Essentials.Ref.any")
                {
                    Version = "6.0.312",
                    Kind = PackKind.Framework,
                    Platforms = [""],
                    Workloads = ["maui", "maui-android", "maui-desktop", "maui-ios", "maui-maccatalyst", "maui-mobile", "maui-tizen", "maui-windows"]
                },
                new PackReference("Microsoft.Maui.Essentials.Ref.ios")
                {
                    Version = "6.0.312",
                    Kind = PackKind.Framework,
                    Platforms = ["ios"],
                    Workloads = ["maui", "maui-ios", "maui-mobile"]
                },
                new PackReference("Microsoft.Maui.Essentials.Ref.maccatalyst")
                {
                    Version = "6.0.312",
                    Kind = PackKind.Framework,
                    Platforms = ["maccatalyst"],
                    Workloads = ["maui", "maui-desktop", "maui-maccatalyst"]
                },
                new PackReference("Microsoft.Maui.Essentials.Ref.tizen")
                {
                    Version = "6.0.312",
                    Kind = PackKind.Framework,
                    Platforms = ["tizen"],
                    Workloads = ["maui", "maui-mobile", "maui-tizen"]
                },
                new PackReference("Microsoft.Maui.Essentials.Ref.win")
                {
                    Version = "6.0.312",
                    Kind = PackKind.Framework,
                    Platforms = ["windows"],
                    Workloads = ["maui-desktop", "maui", "maui-windows"]
                },
                new PackReference("Microsoft.Maui.Extensions")
                {
                    Version = "6.0.312",
                    Kind = PackKind.Library,
                    Workloads = ["maui", "maui-android", "maui-desktop", "maui-ios", "maui-maccatalyst", "maui-mobile", "maui-tizen", "maui-windows"]
                },
                new PackReference("Microsoft.Maui.Graphics")
                {
                    Version = "6.0.300",
                    Kind = PackKind.Library,
                    Workloads = ["maui", "maui-android", "maui-desktop", "maui-ios", "maui-maccatalyst", "maui-mobile", "maui-tizen", "maui-windows"]
                },
                new PackReference("Microsoft.Maui.Graphics.Win2D.WinUI.Desktop")
                {
                    Version = "6.0.300",
                    Kind = PackKind.Library,
                    Workloads = ["maui", "maui-desktop", "maui-windows"]
                },
                new PackReference("Microsoft.tvOS.Ref")
                {
                    Version = "15.4.303",
                    Kind = PackKind.Framework,
                    Platforms = ["tvos"],
                    Workloads = ["tvos"]
                },
            ]
        },
        new FrameworkDefinition("net7.0")
        {
            SupportedPlatforms =
            [
                new FrameworkPlatformDefinition("android")
                {
                    Versions = ["21.0", "22.0", "23.0", "24.0", "25.0", "26.0", "27.0", "28.0", "29.0", "30.0", "31.0", "32.0", "33.0"]
                },
                new FrameworkPlatformDefinition("ios")
                {
                    Versions = ["10.0", "10.1", "10.2", "10.3", "11.0", "11.1", "11.2", "11.3", "11.4", "12.0", "12.1", "12.2", "12.3", "12.4", "13.0", "13.1", "13.2", "13.3", "13.4", "13.5", "13.6", "14.0", "14.1", "14.2", "14.3", "14.4", "14.5", "15.0", "15.2", "15.4", "16.0", "16.1", "16.2", "16.4"]
                },
                new FrameworkPlatformDefinition("maccatalyst")
                {
                    Versions = ["13.1", "13.2", "13.3", "13.4", "13.5", "14.2", "14.3", "14.4", "14.5", "15.0", "15.2", "15.4", "16.1", "16.2", "16.4"]
                },
                new FrameworkPlatformDefinition("macos")
                {
                    Versions = ["10.14", "10.15", "10.16", "11.0", "11.1", "11.2", "11.3", "12.0", "12.1", "12.3", "13.0", "13.1", "13.3"]
                },
                new FrameworkPlatformDefinition("tvos")
                {
                    Versions = ["10.0", "10.1", "10.2", "11.0", "11.1", "11.2", "11.3", "11.4", "12.0", "12.1", "12.2", "12.3", "12.4", "13.0", "13.2", "13.3", "13.4", "14.0", "14.2", "14.3", "14.4", "14.5", "15.0", "15.2", "15.4", "16.0", "16.1", "16.4"]
                },
                new FrameworkPlatformDefinition("windows")
                {
                    Versions = ["7.0", "8.0", "10.0.17763", "10.0.18362", "10.0.19041", "10.0.20348", "10.0.22000", "10.0.22621"]
                }
            ],
            BuiltInPacks =
            [
                new PackReference("Microsoft.NETCore.App.Ref")
                {
                    Version = "7.0",
                    Platforms = [""],
                    Kind = PackKind.Framework
                },
                new PackReference("Microsoft.AspNetCore.App.Ref")
                {
                    Version = "7.0",
                    Platforms = [""],
                    Kind = PackKind.Framework
                },
                new PackReference("Microsoft.WindowsDesktop.App.Ref")
                {
                    Version = "7.0",
                    Platforms = ["windows"],
                    Kind = PackKind.Framework
                },
                new PackReference("Microsoft.Windows.SDK.NET.Ref")
                {
                    Version = "10.0.17763",
                    Platforms = ["windows10.0.17763"],
                    Kind = PackKind.Framework
                },
                new PackReference("Microsoft.Windows.SDK.NET.Ref")
                {
                    Version = "10.0.18362",
                    Platforms = ["windows10.0.18362"],
                    Kind = PackKind.Framework
                },
                new PackReference("Microsoft.Windows.SDK.NET.Ref")
                {
                    Version = "10.0.19041",
                    Platforms = ["windows10.0.19041"],
                    Kind = PackKind.Framework
                },
                new PackReference("Microsoft.Windows.SDK.NET.Ref")
                {
                    Version = "10.0.20348",
                    Platforms = ["windows10.0.20348"],
                    Kind = PackKind.Framework
                },
                new PackReference("Microsoft.Windows.SDK.NET.Ref")
                {
                    Version = "10.0.22000",
                    Platforms = ["windows10.0.22000"],
                    Kind = PackKind.Framework
                },
                new PackReference("Microsoft.Windows.SDK.NET.Ref")
                {
                    Version = "10.0.22621",
                    Platforms = ["windows10.0.22621"],
                    Kind = PackKind.Framework
                }
            ],
            WorkloadPacks =
            [
                new PackReference("Microsoft.Android.Ref.33")
                {
                    Version = "33.0.4",
                    Kind = PackKind.Framework,
                    Platforms = ["android"],
                    Workloads = ["android", "maui", "maui-android", "maui-mobile"]
                },
                new PackReference("Microsoft.AspNetCore.Components.WebView.Maui")
                {
                    Version = "7.0.49",
                    Kind = PackKind.Library,
                    Workloads = ["maui", "maui-android", "maui-desktop", "maui-ios", "maui-maccatalyst", "maui-mobile", "maui-tizen", "maui-windows"]
                },
                new PackReference("Microsoft.iOS.Ref")
                {
                    Version = "16.0.1478",
                    Kind = PackKind.Framework,
                    Platforms = ["ios"],
                    Workloads = ["ios", "maui", "maui-ios", "maui-mobile"]
                },
                new PackReference("Microsoft.MacCatalyst.Ref")
                {
                    Version = "15.4.2372",
                    Kind = PackKind.Framework,
                    Platforms = ["maccatalyst"],
                    Workloads = ["maccatalyst", "maui", "maui-desktop", "maui-maccatalyst"]
                },
                new PackReference("Microsoft.macOS.Ref")
                {
                    Version = "12.3.2372",
                    Kind = PackKind.Framework,
                    Platforms = ["macos"],
                    Workloads = ["macos"]
                },
                new PackReference("Microsoft.Maui.Controls.Ref.android")
                {
                    Version = "7.0.49",
                    Kind = PackKind.Framework,
                    Platforms = ["android"],
                    Workloads = ["maui", "maui-android", "maui-mobile"]
                },
                new PackReference("Microsoft.Maui.Controls.Ref.any")
                {
                    Version = "7.0.49",
                    Kind = PackKind.Framework,
                    Platforms = [""],
                    Workloads = ["maui", "maui-android", "maui-desktop", "maui-ios", "maui-maccatalyst", "maui-mobile", "maui-tizen", "maui-windows"]
                },
                new PackReference("Microsoft.Maui.Controls.Ref.ios")
                {
                    Version = "7.0.49",
                    Kind = PackKind.Framework,
                    Platforms = ["ios"],
                    Workloads = ["maui", "maui-ios", "maui-mobile"]
                },
                new PackReference("Microsoft.Maui.Controls.Ref.maccatalyst")
                {
                    Version = "7.0.49",
                    Kind = PackKind.Framework,
                    Platforms = ["maccatalyst"],
                    Workloads = ["maui", "maui-maccatalyst", "maui-desktop"]
                },
                new PackReference("Microsoft.Maui.Controls.Ref.tizen")
                {
                    Version = "7.0.49",
                    Kind = PackKind.Framework,
                    Platforms = ["tizen"],
                    Workloads = ["maui", "maui-mobile", "maui-tizen"]
                },
                new PackReference("Microsoft.Maui.Controls.Ref.win")
                {
                    Version = "7.0.49",
                    Kind = PackKind.Framework,
                    Platforms = ["windows"],
                    Workloads = ["maui", "maui-desktop", "maui-windows"]
                },
                new PackReference("Microsoft.Maui.Core.Ref.android")
                {
                    Version = "7.0.49",
                    Kind = PackKind.Framework,
                    Platforms = ["android"],
                    Workloads = ["maui", "maui-android", "maui-mobile"]
                },
                new PackReference("Microsoft.Maui.Core.Ref.any")
                {
                    Version = "7.0.49",
                    Kind = PackKind.Framework,
                    Platforms = [""],
                    Workloads = ["maui", "maui-android", "maui-desktop", "maui-ios", "maui-maccatalyst", "maui-mobile", "maui-tizen", "maui-windows"]
                },
                new PackReference("Microsoft.Maui.Core.Ref.ios")
                {
                    Version = "7.0.49",
                    Kind = PackKind.Framework,
                    Platforms = ["ios"],
                    Workloads = ["maui", "maui-ios", "maui-mobile"]
                },
                new PackReference("Microsoft.Maui.Core.Ref.maccatalyst")
                {
                    Version = "7.0.49",
                    Kind = PackKind.Framework,
                    Platforms = ["maccatalyst"],
                    Workloads = ["maui", "maui-desktop", "maui-maccatalyst"]
                },
                new PackReference("Microsoft.Maui.Core.Ref.tizen")
                {
                    Version = "7.0.49",
                    Kind = PackKind.Framework,
                    Platforms = ["tizen"],
                    Workloads = ["maui", "maui-mobile", "maui-tizen"]
                },
                new PackReference("Microsoft.Maui.Core.Ref.win")
                {
                    Version = "7.0.49",
                    Kind = PackKind.Framework,
                    Platforms = ["ios"],
                    Workloads = ["maui", "maui-desktop", "maui-windows"]
                },
                new PackReference("Microsoft.Maui.Essentials.Ref.android")
                {
                    Version = "7.0.49",
                    Kind = PackKind.Framework,
                    Platforms = ["android"],
                    Workloads = ["maui", "maui-android", "maui-mobile"]
                },
                new PackReference("Microsoft.Maui.Essentials.Ref.any")
                {
                    Version = "7.0.49",
                    Kind = PackKind.Framework,
                    Platforms = [""],
                    Workloads = ["maui", "maui-android", "maui-desktop", "maui-ios", "maui-maccatalyst", "maui-mobile", "maui-tizen", "maui-windows"]
                },
                new PackReference("Microsoft.Maui.Essentials.Ref.ios")
                {
                    Version = "7.0.49",
                    Kind = PackKind.Framework,
                    Platforms = ["ios"],
                    Workloads = ["maui", "maui-ios", "maui-mobile"]
                },
                new PackReference("Microsoft.Maui.Essentials.Ref.maccatalyst")
                {
                    Version = "7.0.49",
                    Kind = PackKind.Framework,
                    Platforms = ["maccatalyst"],
                    Workloads = ["maui", "maui-desktop", "maui-maccatalyst"]
                },
                new PackReference("Microsoft.Maui.Essentials.Ref.tizen")
                {
                    Version = "7.0.49",
                    Kind = PackKind.Framework,
                    Platforms = ["tizen"],
                    Workloads = ["maui", "maui-mobile", "maui-tizen"]
                },
                new PackReference("Microsoft.Maui.Essentials.Ref.win")
                {
                    Version = "7.0.49",
                    Kind = PackKind.Framework,
                    Platforms = ["windows"],
                    Workloads = ["maui", "maui-desktop", "maui-windows"]
                },
                new PackReference("Microsoft.Maui.Graphics")
                {
                    Version = "7.0.49",
                    Kind = PackKind.Library,
                    Workloads = ["maui", "maui-android", "maui-desktop", "maui-ios", "maui-maccatalyst", "maui-mobile", "maui-tizen", "maui-windows"]
                },
                new PackReference("Microsoft.Maui.Graphics.Win2D.WinUI.Desktop")
                {
                    Version = "7.0.49",
                    Kind = PackKind.Library,
                    Workloads = ["maui", "maui-desktop", "maui-windows"]
                },
                new PackReference("Microsoft.tvOS.Ref")
                {
                    Version = "16.0.1478",
                    Kind = PackKind.Framework,
                    Platforms = ["tvos"],
                    Workloads = ["tvos"]
                },
            ]
        },
        new FrameworkDefinition("net8.0")
        {
            SupportedPlatforms =
            [
                new FrameworkPlatformDefinition("android")
                {
                    Versions = ["21.0", "22.0", "23.0", "24.0", "25.0", "26.0", "27.0", "28.0", "29.0", "30.0", "31.0", "32.0", "33.0", "34.0"]
                },
                new FrameworkPlatformDefinition("ios")
                {
                    Versions = ["10.0", "10.1", "10.2", "10.3", "11.0", "11.1", "11.2", "11.3", "11.4", "12.0", "12.1", "12.2", "12.3", "12.4", "13.0", "13.1", "13.2", "13.3", "13.4", "13.5", "13.6", "14.0", "14.1", "14.2", "14.3", "14.4", "14.5", "15.0", "15.2", "15.4", "16.0", "16.1", "16.2", "16.4", "17.0", "17.2"]
                },
                new FrameworkPlatformDefinition("maccatalyst")
                {
                    Versions = ["13.1", "13.2", "13.3", "13.4", "13.5", "14.2", "14.3", "14.4", "14.5", "15.0", "15.2", "15.4", "16.1", "16.2", "16.4", "17.0", "17.2"]
                },
                new FrameworkPlatformDefinition("macos")
                {
                    Versions = ["10.14", "10.15", "10.16", "11.0", "11.1", "11.2", "11.3", "12.0", "12.1", "12.3", "13.0", "13.1", "13.3", "14.0", "14.2"]
                },
                new FrameworkPlatformDefinition("tvos")
                {
                    Versions = ["10.0", "10.1", "10.2", "11.0", "11.1", "11.2", "11.3", "11.4", "12.0", "12.1", "12.2", "12.3", "12.4", "13.0", "13.2", "13.3", "13.4", "14.0", "14.2", "14.3", "14.4", "14.5", "15.0", "15.2", "15.4", "16.0", "16.1", "16.4", "17.0", "17.2"]
                },
                new FrameworkPlatformDefinition("windows")
                {
                    Versions = ["7.0", "8.0", "10.0.17763", "10.0.18362", "10.0.19041", "10.0.20348", "10.0.22000", "10.0.22621"]
                }
            ],
            BuiltInPacks =
            [
                new PackReference("Microsoft.NETCore.App.Ref")
                {
                    Version = "8.0",
                    Platforms = [""],
                    Kind = PackKind.Framework
                },
                new PackReference("Microsoft.AspNetCore.App.Ref")
                {
                    Version = "8.0",
                    Platforms = [""],
                    Kind = PackKind.Framework
                },
                new PackReference("Microsoft.WindowsDesktop.App.Ref")
                {
                    Version = "8.0",
                    Platforms = ["windows"],
                    Kind = PackKind.Framework
                },
                new PackReference("Microsoft.Windows.SDK.NET.Ref")
                {
                    Version = "10.0.17763",
                    Platforms = ["windows10.0.17763"],
                    Kind = PackKind.Framework
                },
                new PackReference("Microsoft.Windows.SDK.NET.Ref")
                {
                    Version = "10.0.18362",
                    Platforms = ["windows10.0.18362"],
                    Kind = PackKind.Framework
                },
                new PackReference("Microsoft.Windows.SDK.NET.Ref")
                {
                    Version = "10.0.19041",
                    Platforms = ["windows10.0.19041"],
                    Kind = PackKind.Framework
                },
                new PackReference("Microsoft.Windows.SDK.NET.Ref")
                {
                    Version = "10.0.20348",
                    Platforms = ["windows10.0.20348"],
                    Kind = PackKind.Framework
                },
                new PackReference("Microsoft.Windows.SDK.NET.Ref")
                {
                    Version = "10.0.22000",
                    Platforms = ["windows10.0.22000"],
                    Kind = PackKind.Framework
                },
                new PackReference("Microsoft.Windows.SDK.NET.Ref")
                {
                    Version = "10.0.22621",
                    Platforms = ["windows10.0.22621"],
                    Kind = PackKind.Framework
                }
            ],
            WorkloadPacks =
            [
                new PackReference("Aspire.Hosting")
                {
                    Version = "8.0.0-preview.3.24105.21",
                    Kind = PackKind.Library,
                    Workloads = ["aspire"]
                },
                new PackReference("Microsoft.Android.Ref.34")
                {
                    Version = "34.0.79",
                    Kind = PackKind.Framework,
                    Platforms = ["android"],
                    Workloads = ["android", "maui", "maui-android", "maui-mobile"]
                },
                new PackReference("Microsoft.AspNetCore.Components.WebView.Maui")
                {
                    Version = "8.0.6",
                    Kind = PackKind.Library,
                    Workloads = ["maui", "maui-android", "maui-desktop", "maui-ios", "maui-maccatalyst", "maui-mobile", "maui-tizen", "maui-windows"]
                },
                new PackReference("Microsoft.iOS.Ref")
                {
                    Version = "17.2.8022",
                    Kind = PackKind.Framework,
                    Platforms = ["ios"],
                    Workloads = ["ios", "maui", "maui-ios", "maui-mobile"]
                },
                new PackReference("Microsoft.MacCatalyst.Ref")
                {
                    Version = "17.2.8022",
                    Kind = PackKind.Framework,
                    Platforms = ["maccatalyst"],
                    Workloads = ["maccatalyst", "maui", "maui-desktop", "maui-maccatalyst"]
                },
                new PackReference("Microsoft.macOS.Ref")
                {
                    Version = "14.2.8022",
                    Kind = PackKind.Framework,
                    Platforms = ["macos"],
                    Workloads = ["macos"]
                },
                new PackReference("Microsoft.Maui.Controls")
                {
                    Version = "8.0.6",
                    Kind = PackKind.Library,
                    Workloads = ["maui", "maui-android", "maui-desktop", "maui-ios", "maui-maccatalyst", "maui-mobile", "maui-tizen", "maui-windows"]
                },
                new PackReference("Microsoft.Maui.Controls.Build.Tasks")
                {
                    Version = "8.0.6",
                    Kind = PackKind.Library,
                    Workloads = ["maui", "maui-android", "maui-desktop", "maui-ios", "maui-maccatalyst", "maui-mobile", "maui-tizen", "maui-windows"]
                },
                new PackReference("Microsoft.Maui.Controls.Compatibility")
                {
                    Version = "8.0.6",
                    Kind = PackKind.Library,
                    Workloads = ["maui", "maui-android", "maui-desktop", "maui-ios", "maui-maccatalyst", "maui-mobile", "maui-tizen", "maui-windows"]
                },
                new PackReference("Microsoft.Maui.Controls.Core")
                {
                    Version = "8.0.6",
                    Kind = PackKind.Library,
                    Workloads = ["maui", "maui-android", "maui-desktop", "maui-ios", "maui-maccatalyst", "maui-mobile", "maui-tizen", "maui-windows"]
                },
                new PackReference("Microsoft.Maui.Controls.Xaml")
                {
                    Version = "8.0.6",
                    Kind = PackKind.Library,
                    Workloads = ["maui", "maui-android", "maui-desktop", "maui-ios", "maui-maccatalyst", "maui-mobile", "maui-tizen", "maui-windows"]
                },
                new PackReference("Microsoft.Maui.Core")
                {
                    Version = "8.0.6",
                    Kind = PackKind.Library,
                    Workloads = ["maui", "maui-android", "maui-desktop", "maui-ios", "maui-maccatalyst", "maui-mobile", "maui-tizen", "maui-windows"]
                },
                new PackReference("Microsoft.Maui.Essentials")
                {
                    Version = "8.0.6",
                    Kind = PackKind.Library,
                    Workloads = ["maui", "maui-android", "maui-desktop", "maui-ios", "maui-maccatalyst", "maui-mobile", "maui-tizen", "maui-windows"]
                },
                new PackReference("Microsoft.Maui.Graphics")
                {
                    Version = "8.0.6",
                    Kind = PackKind.Library,
                    Workloads = ["maui", "maui-android", "maui-desktop", "maui-ios", "maui-maccatalyst", "maui-mobile", "maui-tizen", "maui-windows"]
                },
                new PackReference("Microsoft.Maui.Graphics.Win2D.WinUI.Desktop")
                {
                    Version = "8.0.6",
                    Kind = PackKind.Library,
                    Workloads = ["maui", "maui-desktop", "maui-windows"]
                },
                new PackReference("Microsoft.Maui.Resizetizer")
                {
                    Version = "8.0.6",
                    Kind = PackKind.Library,
                    Workloads = ["maui", "maui-android", "maui-desktop", "maui-ios", "maui-maccatalyst", "maui-mobile", "maui-tizen", "maui-windows"]
                },
                new PackReference("Microsoft.tvOS.Ref")
                {
                    Version = "17.2.8022",
                    Kind = PackKind.Framework,
                    Platforms = ["tvos"],
                    Workloads = ["tvos"]
                },
            ]
        },
        new FrameworkDefinition("net9.0", isPreview: true)
        {
            SupportedPlatforms =
            [
                new FrameworkPlatformDefinition("android")
                {
                    Versions = ["21.0", "22.0", "23.0", "24.0", "25.0", "26.0", "27.0", "28.0", "29.0", "30.0", "31.0", "32.0", "33.0", "34.0"]
                },
                new FrameworkPlatformDefinition("ios")
                {
                    Versions = ["10.0", "10.1", "10.2", "10.3", "11.0", "11.1", "11.2", "11.3", "11.4", "12.0", "12.1", "12.2", "12.3", "12.4", "13.0", "13.1", "13.2", "13.3", "13.4", "13.5", "13.6", "14.0", "14.1", "14.2", "14.3", "14.4", "14.5", "15.0", "15.2", "15.4", "16.0", "16.1", "16.2", "16.4", "17.0", "17.2"]
                },
                new FrameworkPlatformDefinition("maccatalyst")
                {
                    Versions = ["13.1", "13.2", "13.3", "13.4", "13.5", "14.2", "14.3", "14.4", "14.5", "15.0", "15.2", "15.4", "16.1", "16.2", "16.4", "17.0", "17.2"]
                },
                new FrameworkPlatformDefinition("macos")
                {
                    Versions = ["10.14", "10.15", "10.16", "11.0", "11.1", "11.2", "11.3", "12.0", "12.1", "12.3", "13.0", "13.1", "13.3", "14.0", "14.2"]
                },
                new FrameworkPlatformDefinition("tvos")
                {
                    Versions = ["10.0", "10.1", "10.2", "11.0", "11.1", "11.2", "11.3", "11.4", "12.0", "12.1", "12.2", "12.3", "12.4", "13.0", "13.2", "13.3", "13.4", "14.0", "14.2", "14.3", "14.4", "14.5", "15.0", "15.2", "15.4", "16.0", "16.1", "16.4", "17.0", "17.2"]
                },
                new FrameworkPlatformDefinition("windows")
                {
                    Versions = ["7.0", "8.0", "10.0.17763", "10.0.18362", "10.0.19041", "10.0.20348", "10.0.22000", "10.0.22621"]
                }
            ],
            BuiltInPacks =
            [
                new PackReference("Microsoft.NETCore.App.Ref")
                {
                    Version = "9.0",
                    Platforms = [""],
                    Kind = PackKind.Framework
                },
                new PackReference("Microsoft.AspNetCore.App.Ref")
                {
                    Version = "9.0",
                    Platforms = [""],
                    Kind = PackKind.Framework
                },
                new PackReference("Microsoft.WindowsDesktop.App.Ref")
                {
                    Version = "9.0",
                    Platforms = ["windows"],
                    Kind = PackKind.Framework
                },
                new PackReference("Microsoft.Windows.SDK.NET.Ref")
                {
                    Version = "10.0.17763",
                    Platforms = ["windows10.0.17763"],
                    Kind = PackKind.Framework
                },
                new PackReference("Microsoft.Windows.SDK.NET.Ref")
                {
                    Version = "10.0.18362",
                    Platforms = ["windows10.0.18362"],
                    Kind = PackKind.Framework
                },
                new PackReference("Microsoft.Windows.SDK.NET.Ref")
                {
                    Version = "10.0.19041",
                    Platforms = ["windows10.0.19041"],
                    Kind = PackKind.Framework
                },
                new PackReference("Microsoft.Windows.SDK.NET.Ref")
                {
                    Version = "10.0.20348",
                    Platforms = ["windows10.0.20348"],
                    Kind = PackKind.Framework
                },
                new PackReference("Microsoft.Windows.SDK.NET.Ref")
                {
                    Version = "10.0.22000",
                    Platforms = ["windows10.0.22000"],
                    Kind = PackKind.Framework
                },
                new PackReference("Microsoft.Windows.SDK.NET.Ref")
                {
                    Version = "10.0.22621",
                    Platforms = ["windows10.0.22621"],
                    Kind = PackKind.Framework
                }
            ],
            WorkloadPacks =
            [
                new PackReference("Aspire.Hosting")
                {
                    Version = "9.0",
                    Kind = PackKind.Library,
                    Workloads = ["aspire"]
                },
                new PackReference("Microsoft.Android.Ref.34")
                {
                    Version = "34.99",
                    Kind = PackKind.Framework,
                    Platforms = ["android"],
                    Workloads = ["android", "maui", "maui-android", "maui-mobile"]
                },
                new PackReference("Microsoft.AspNetCore.Components.WebView.Maui")
                {
                    Version = "9.0",
                    Kind = PackKind.Library,
                    Workloads = ["maui", "maui-android", "maui-desktop", "maui-ios", "maui-maccatalyst", "maui-mobile", "maui-tizen", "maui-windows"]
                },
                new PackReference("Microsoft.iOS.Ref")
                {
                    Version = "17.2",
                    Kind = PackKind.Framework,
                    Platforms = ["ios"],
                    Workloads = ["ios", "maui", "maui-ios", "maui-mobile"]
                },
                new PackReference("Microsoft.MacCatalyst.Ref")
                {
                    Version = "17.2",
                    Kind = PackKind.Framework,
                    Platforms = ["maccatalyst"],
                    Workloads = ["maccatalyst", "maui", "maui-desktop", "maui-maccatalyst"]
                },
                new PackReference("Microsoft.macOS.Ref")
                {
                    Version = "14.2",
                    Kind = PackKind.Framework,
                    Platforms = ["macos"],
                    Workloads = ["macos"]
                },
                new PackReference("Microsoft.Maui.Controls")
                {
                    Version = "9.0",
                    Kind = PackKind.Library,
                    Workloads = ["maui", "maui-android", "maui-desktop", "maui-ios", "maui-maccatalyst", "maui-mobile", "maui-tizen", "maui-windows"]
                },
                new PackReference("Microsoft.Maui.Controls.Build.Tasks")
                {
                    Version = "9.0",
                    Kind = PackKind.Library,
                    Workloads = ["maui", "maui-android", "maui-desktop", "maui-ios", "maui-maccatalyst", "maui-mobile", "maui-tizen", "maui-windows"]
                },
                new PackReference("Microsoft.Maui.Controls.Compatibility")
                {
                    Version = "9.0",
                    Kind = PackKind.Library,
                    Workloads = ["maui", "maui-android", "maui-desktop", "maui-ios", "maui-maccatalyst", "maui-mobile", "maui-tizen", "maui-windows"]
                },
                new PackReference("Microsoft.Maui.Controls.Core")
                {
                    Version = "9.0",
                    Kind = PackKind.Library,
                    Workloads = ["maui", "maui-android", "maui-desktop", "maui-ios", "maui-maccatalyst", "maui-mobile", "maui-tizen", "maui-windows"]
                },
                new PackReference("Microsoft.Maui.Controls.Xaml")
                {
                    Version = "9.0",
                    Kind = PackKind.Library,
                    Workloads = ["maui", "maui-android", "maui-desktop", "maui-ios", "maui-maccatalyst", "maui-mobile", "maui-tizen", "maui-windows"]
                },
                new PackReference("Microsoft.Maui.Core")
                {
                    Version = "9.0",
                    Kind = PackKind.Library,
                    Workloads = ["maui", "maui-android", "maui-desktop", "maui-ios", "maui-maccatalyst", "maui-mobile", "maui-tizen", "maui-windows"]
                },
                new PackReference("Microsoft.Maui.Essentials")
                {
                    Version = "9.0",
                    Kind = PackKind.Library,
                    Workloads = ["maui", "maui-android", "maui-desktop", "maui-ios", "maui-maccatalyst", "maui-mobile", "maui-tizen", "maui-windows"]
                },
                new PackReference("Microsoft.Maui.Graphics")
                {
                    Version = "9.0",
                    Kind = PackKind.Library,
                    Workloads = ["maui", "maui-android", "maui-desktop", "maui-ios", "maui-maccatalyst", "maui-mobile", "maui-tizen", "maui-windows"]
                },
                new PackReference("Microsoft.Maui.Graphics.Win2D.WinUI.Desktop")
                {
                    Version = "9.0",
                    Kind = PackKind.Library,
                    Workloads = ["maui", "maui-desktop", "maui-windows"]
                },
                new PackReference("Microsoft.Maui.Resizetizer")
                {
                    Version = "9.0",
                    Kind = PackKind.Library,
                    Workloads = ["maui", "maui-android", "maui-desktop", "maui-ios", "maui-maccatalyst", "maui-mobile", "maui-tizen", "maui-windows"]
                },
                new PackReference("Microsoft.tvOS.Ref")
                {
                    Version = "17.2",
                    Kind = PackKind.Framework,
                    Platforms = ["tvos"],
                    Workloads = ["tvos"]
                },
            ]
        },
        ..LoadDumpPackManifest()
    ];

    private static IReadOnlyList<FrameworkDefinition> LoadDumpPackManifest()
    {
        var jsonFile = ResolveDumpPackManifestPath();

        if (jsonFile is null)
            return [];

        var jsonContent = File.ReadAllText(jsonFile);
        var settings = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        var manifest = JsonSerializer.Deserialize<DumpPackManifest>(jsonContent, settings);
        if (manifest is null)
            return [];

        var result = new List<FrameworkDefinition>();

        var frameworkVersions = manifest.WorkLoadPackManifests
            .Select(w => w.DotNetVersion)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(v => v, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var frameworkVersion in frameworkVersions)
        {
            if (IsFrameworkInPredefinedList(frameworkVersion))
                continue;

            var builtInPacks = ConvertBuiltInPacks(frameworkVersion, manifest.BuiltInPackManifests);
            var workloadManifest = manifest.WorkLoadPackManifests
                .FirstOrDefault(w => frameworkVersion.Equals(w.DotNetVersion, StringComparison.OrdinalIgnoreCase));
            var workloadPacks = ConvertWorkloadPacks(workloadManifest);

            if (builtInPacks.Count == 0 && workloadPacks.Count == 0)
                continue;

            var supportedPlatforms = ConvertSupportedPlatforms(frameworkVersion, manifest.BuiltInPackManifests, workloadManifest, workloadPacks);

            var frameworkDefinition = new FrameworkDefinition(frameworkVersion)
            {
                SupportedPlatforms = supportedPlatforms,
                BuiltInPacks = builtInPacks,
                WorkloadPacks = workloadPacks
            };

            result.Add(frameworkDefinition);
        }

        return result;

        static IReadOnlyList<PackReference> ConvertBuiltInPacks(string tfm, IEnumerable<BuiltInPackManifest> manifests)
        {
            var packs = new List<PackReference>();
            foreach (var manifest in manifests)
            {
                var references = manifest.FrameworkReferences
                    .Where(r => r.TargetFramework.Equals(tfm, StringComparison.OrdinalIgnoreCase) ||
                                r.TargetFramework.StartsWith(tfm + "-", StringComparison.OrdinalIgnoreCase));

                foreach (var reference in references)
                {
                    var platform = reference.TargetFramework.StartsWith(tfm + "-", StringComparison.OrdinalIgnoreCase)
                        ? reference.TargetFramework[(tfm.Length + 1)..]
                        : "";

                    foreach (var pack in reference.Packs)
                    {
                        var computedPlatform = string.IsNullOrEmpty(platform)
                            ? InferBuiltInPlatform(pack.PackName)
                            : platform;

                        packs.Add(new PackReference(pack.PackName)
                        {
                            Version = pack.PackVersion,
                            Kind = PackKind.Framework,
                            Platforms = [computedPlatform]
                        });
                    }
                }
            }

            return packs.GroupBy(p => (p.Name, p.Version, Platform: string.Join("|", p.Platforms)))
                        .Select(g => g.First())
                        .ToArray();
        }

        static IReadOnlyList<PackReference> ConvertWorkloadPacks(WorkLoadPackManifest? manifest)
        {
            if (manifest is null)
                return [];

            var packs = new List<PackReference>();

            foreach (var pack in manifest.Packs)
            {
                if (!Enum.TryParse<PackKind>(pack.PackKind, ignoreCase: true, out var kind))
                    continue;

                // Library packs must not list platforms in FrameworkDefinition.
                var platforms = kind == PackKind.Framework
                    ? InferWorkloadPlatforms(pack.PackName, pack.WorkloadNames).ToArray()
                    : [];

                packs.Add(new PackReference(pack.PackName)
                {
                    Version = pack.PackVersion,
                    Kind = kind,
                    Platforms = platforms,
                    Workloads = [..pack.WorkloadNames]
                });
            }

            return packs;
        }

        static IReadOnlyList<FrameworkPlatformDefinition> ConvertSupportedPlatforms(string tfm,
                                                                                    IEnumerable<BuiltInPackManifest> builtInManifests,
                                                                                    WorkLoadPackManifest? workloadManifest,
                                                                                    IReadOnlyList<PackReference> workloadPacks)
        {
            IEnumerable<PlatformVersion>? source = null;

            if ((workloadManifest?.PlatformVersions?.Count ?? 0) > 0)
            {
                source = workloadManifest!.PlatformVersions;
            }
            else
            {
                var firstBuiltInWithTfm = builtInManifests.FirstOrDefault(m =>
                    m.FrameworkReferences.Any(r => r.TargetFramework.Equals(tfm, StringComparison.OrdinalIgnoreCase) ||
                                                   r.TargetFramework.StartsWith(tfm + "-", StringComparison.OrdinalIgnoreCase)));

                source = firstBuiltInWithTfm?.PlatformVersions;
            }

            if (source is null)
                return [];

            var versionByPlatform = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var platform in source)
            {
                var normalizedVersions = platform.Versions
                    .Select(v => NormalizePlatformVersion(platform.Platform, v))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(v => v, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                if (normalizedVersions.Length == 0)
                    continue;

                versionByPlatform[platform.Platform.ToLowerInvariant()] = new HashSet<string>(normalizedVersions, StringComparer.OrdinalIgnoreCase);
            }

            // Add missing platforms referenced by framework workload packs and infer at least one version.
            foreach (var pack in workloadPacks.Where(p => p.Kind == PackKind.Framework))
            {
                foreach (var platform in pack.Platforms)
                {
                    if (string.IsNullOrWhiteSpace(platform))
                        continue;

                    if (!versionByPlatform.TryGetValue(platform, out var versions))
                    {
                        versions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        versionByPlatform.Add(platform, versions);
                    }

                    if (TryInferPlatformVersion(platform, pack, out var inferredVersion))
                        versions.Add(inferredVersion);
                }
            }

            var result = new List<FrameworkPlatformDefinition>();

            foreach (var (platform, versions) in versionByPlatform)
            {
                if (versions.Count == 0)
                    continue;

                result.Add(new FrameworkPlatformDefinition(platform)
                {
                    Versions = versions.OrderBy(v => v, StringComparer.OrdinalIgnoreCase).ToArray()
                });
            }

            return result;
        }

        static bool TryInferPlatformVersion(string platform, PackReference pack, out string version)
        {
            // iOS/macOS/MacCatalyst/tvOS packs often encode platform version in name: ...net10.0_26.2
            var underscoreIndex = pack.Name.LastIndexOf('_');
            if (underscoreIndex >= 0 && underscoreIndex < pack.Name.Length - 1)
            {
                var fromName = pack.Name[(underscoreIndex + 1)..];
                if (Version.TryParse(fromName, out var parsed))
                {
                    version = NormalizePlatformVersion(platform, fromName);
                    return true;
                }
            }

            // Android packs often encode API level in name: Microsoft.Android.Ref.36
            if (platform.Equals("android", StringComparison.OrdinalIgnoreCase))
            {
                var segments = pack.Name.Split('.');
                if (segments.Length > 0 && int.TryParse(segments[^1], out var apiLevel))
                {
                    version = $"{apiLevel}.0";
                    return true;
                }
            }

            // Fallback to major.minor of package version.
            if (Version.TryParse(pack.Version, out var packageVersion))
            {
                version = NormalizePlatformVersion(platform, $"{packageVersion.Major}.{packageVersion.Minor}");
                return true;
            }

            version = string.Empty;
            return false;
        }

        static string InferBuiltInPlatform(string packName)
        {
            if (packName.Equals("Microsoft.WindowsDesktop.App.Ref", StringComparison.OrdinalIgnoreCase))
                return "windows";

            return "";
        }

        static IEnumerable<string> InferWorkloadPlatforms(string packName, IEnumerable<string> workloads)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var workload in workloads)
            {
                if (workload.Contains("android", StringComparison.OrdinalIgnoreCase))
                    result.Add("android");
                if (workload.Contains("ios", StringComparison.OrdinalIgnoreCase))
                    result.Add("ios");
                if (workload.Contains("maccatalyst", StringComparison.OrdinalIgnoreCase))
                    result.Add("maccatalyst");
                if (workload.Contains("macos", StringComparison.OrdinalIgnoreCase))
                    result.Add("macos");
                if (workload.Contains("tvos", StringComparison.OrdinalIgnoreCase))
                    result.Add("tvos");
                if (workload.Contains("windows", StringComparison.OrdinalIgnoreCase))
                    result.Add("windows");
            }

            if (packName.Contains("Android", StringComparison.OrdinalIgnoreCase))
                result.Add("android");
            if (packName.Contains("iOS", StringComparison.OrdinalIgnoreCase))
                result.Add("ios");
            if (packName.Contains("MacCatalyst", StringComparison.OrdinalIgnoreCase))
                result.Add("maccatalyst");
            if (packName.Contains("macOS", StringComparison.OrdinalIgnoreCase))
                result.Add("macos");
            if (packName.Contains("tvOS", StringComparison.OrdinalIgnoreCase))
                result.Add("tvos");
            if (packName.Contains("Win", StringComparison.OrdinalIgnoreCase) ||
                packName.Contains("Windows", StringComparison.OrdinalIgnoreCase))
                result.Add("windows");

            return result;
        }

        static string NormalizePlatformVersion(string platform, string version)
        {
            if (platform.Equals("Windows", StringComparison.OrdinalIgnoreCase) && Version.TryParse(version, out var parsed))
            {
                if (parsed.Build >= 0)
                    return $"{parsed.Major}.{parsed.Minor}.{parsed.Build}";

                return $"{parsed.Major}.{parsed.Minor}";
            }

            return version;
        }

        static bool IsFrameworkInPredefinedList(string frameworkVersion)
        {
            return frameworkVersion.Equals("netcoreapp3.0", StringComparison.OrdinalIgnoreCase)
                || frameworkVersion.Equals("netcoreapp3.1", StringComparison.OrdinalIgnoreCase)
                || frameworkVersion.Equals("net5.0", StringComparison.OrdinalIgnoreCase)
                || frameworkVersion.Equals("net6.0", StringComparison.OrdinalIgnoreCase)
                || frameworkVersion.Equals("net7.0", StringComparison.OrdinalIgnoreCase)
                || frameworkVersion.Equals("net8.0", StringComparison.OrdinalIgnoreCase)
                || frameworkVersion.Equals("net9.0", StringComparison.OrdinalIgnoreCase);
        }

        static string? ResolveDumpPackManifestPath()
        {
            var candidateRoots = new[]
            {
                Directory.GetCurrentDirectory(),
                AppContext.BaseDirectory
            };

            foreach (var root in candidateRoots)
            {
                var current = new DirectoryInfo(root);

                while (current is not null)
                {
                    var srcDumpPacksPath = Path.Combine(current.FullName, "src", "DumpPacks", "dumppack_output.json");
                    if (File.Exists(srcDumpPacksPath))
                        return srcDumpPacksPath;

                    // Fallback for local ad-hoc runs directly from src/DumpPacks.
                    var directPath = Path.Combine(current.FullName, "dumppack_output.json");
                    if (File.Exists(directPath))
                        return directPath;

                    current = current.Parent;
                }
            }

            return null;
        }
    }
}