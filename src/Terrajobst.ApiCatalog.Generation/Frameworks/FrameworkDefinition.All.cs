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
                    Versions = ["7.0", "8.0", "10.0.17763.0", "10.0.18362.0", "10.0.19041.0", "10.0.20348.0", "10.0.22000.0"]
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
                    Versions = ["7.0", "8.0", "10.0.17763.0", "10.0.18362.0", "10.0.19041.0", "10.0.20348.0", "10.0.22000.0", "10.0.22621.0"]
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
                    Versions = ["7.0", "8.0", "10.0.17763.0", "10.0.18362.0", "10.0.19041.0", "10.0.20348.0", "10.0.22000.0", "10.0.22621.0"]
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
                    Versions = ["7.0", "8.0", "10.0.17763.0", "10.0.18362.0", "10.0.19041.0", "10.0.20348.0", "10.0.22000.0", "10.0.22621.0"]
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
            // TODO: Get SupportedPlatforms for net9.0
            //
            // I copy and pasted the list below from net8.0. We should install net9.0 + workloads
            // and run DumpPacks to get the actual list.
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
                    Versions = ["7.0", "8.0", "10.0.17763.0", "10.0.18362.0", "10.0.19041.0", "10.0.20348.0", "10.0.22000.0", "10.0.22621.0"]
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
                }
            ],
            WorkloadPacks =
            [
                new PackReference("Aspire.Hosting")
                {
                    Version = "9.0.0",
                    Kind = PackKind.Library,
                    Workloads = ["aspire"]
                },
                new PackReference("Microsoft.Android.Ref.34")
                {
                    Version = "34.99.0",
                    Kind = PackKind.Framework,
                    Platforms = ["android"],
                    Workloads = ["android", "maui", "maui-android", "maui-mobile"]
                },
                new PackReference("Microsoft.AspNetCore.Components.WebView.Maui")
                {
                    Version = "9.0.0",
                    Kind = PackKind.Library,
                    Workloads = ["maui", "maui-android", "maui-desktop", "maui-ios", "maui-maccatalyst", "maui-mobile", "maui-tizen", "maui-windows"]
                },
                new PackReference("Microsoft.iOS.Ref")
                {
                    Version = "17.0.0",
                    Kind = PackKind.Framework,
                    Platforms = ["ios"],
                    Workloads = ["ios", "maui", "maui-ios", "maui-mobile"]
                },
                new PackReference("Microsoft.MacCatalyst.Ref")
                {
                    Version = "17.0.0",
                    Kind = PackKind.Framework,
                    Platforms = ["maccatalyst"],
                    Workloads = ["maccatalyst", "maui", "maui-desktop", "maui-maccatalyst"]
                },
                new PackReference("Microsoft.macOS.Ref")
                {
                    Version = "14.0.0",
                    Kind = PackKind.Framework,
                    Platforms = ["macos"],
                    Workloads = ["macos"]
                },
                new PackReference("Microsoft.Maui.Controls")
                {
                    Version = "9.0.0",
                    Kind = PackKind.Library,
                    Workloads = ["maui", "maui-android", "maui-desktop", "maui-ios", "maui-maccatalyst", "maui-mobile", "maui-tizen", "maui-windows"]
                },
                new PackReference("Microsoft.Maui.Controls.Build.Tasks")
                {
                    Version = "9.0.0",
                    Kind = PackKind.Library,
                    Workloads = ["maui", "maui-android", "maui-desktop", "maui-ios", "maui-maccatalyst", "maui-mobile", "maui-tizen", "maui-windows"]
                },
                new PackReference("Microsoft.Maui.Controls.Compatibility")
                {
                    Version = "9.0.0",
                    Kind = PackKind.Library,
                    Workloads = ["maui", "maui-android", "maui-desktop", "maui-ios", "maui-maccatalyst", "maui-mobile", "maui-tizen", "maui-windows"]
                },
                new PackReference("Microsoft.Maui.Controls.Core")
                {
                    Version = "9.0.0",
                    Kind = PackKind.Library,
                    Workloads = ["maui", "maui-android", "maui-desktop", "maui-ios", "maui-maccatalyst", "maui-mobile", "maui-tizen", "maui-windows"]
                },
                new PackReference("Microsoft.Maui.Controls.Xaml")
                {
                    Version = "9.0.0",
                    Kind = PackKind.Library,
                    Workloads = ["maui", "maui-android", "maui-desktop", "maui-ios", "maui-maccatalyst", "maui-mobile", "maui-tizen", "maui-windows"]
                },
                new PackReference("Microsoft.Maui.Core")
                {
                    Version = "9.0.0",
                    Kind = PackKind.Library,
                    Workloads = ["maui", "maui-android", "maui-desktop", "maui-ios", "maui-maccatalyst", "maui-mobile", "maui-tizen", "maui-windows"]
                },
                new PackReference("Microsoft.Maui.Essentials")
                {
                    Version = "9.0.0",
                    Kind = PackKind.Library,
                    Workloads = ["maui", "maui-android", "maui-desktop", "maui-ios", "maui-maccatalyst", "maui-mobile", "maui-tizen", "maui-windows"]
                },
                new PackReference("Microsoft.Maui.Graphics")
                {
                    Version = "9.0.0",
                    Kind = PackKind.Library,
                    Workloads = ["maui", "maui-android", "maui-desktop", "maui-ios", "maui-maccatalyst", "maui-mobile", "maui-tizen", "maui-windows"]
                },
                new PackReference("Microsoft.Maui.Graphics.Win2D.WinUI.Desktop")
                {
                    Version = "9.0.0",
                    Kind = PackKind.Library,
                    Workloads = ["maui", "maui-desktop", "maui-windows"]
                },
                new PackReference("Microsoft.Maui.Resizetizer")
                {
                    Version = "9.0.0",
                    Kind = PackKind.Library,
                    Workloads = ["maui", "maui-android", "maui-desktop", "maui-ios", "maui-maccatalyst", "maui-mobile", "maui-tizen", "maui-windows"]
                },
                new PackReference("Microsoft.tvOS.Ref")
                {
                    Version = "17.0.9712",
                    Kind = PackKind.Framework,
                    Platforms = ["tvos"],
                    Workloads = ["tvos"]
                },
            ]
        }
    ];
}