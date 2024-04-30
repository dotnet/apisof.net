namespace Terrajobst.ApiCatalog;

// Here are the supported platform versions for each .NET version:
//
// net6.0
// Android: 21.0, 22.0, 23.0, 24.0, 25.0, 26.0, 27.0, 28.0, 29.0, 30.0, 31.0, 32.0, 33.0
// iOS: 10.0, 10.1, 10.2, 10.3, 11.0, 11.1, 11.2, 11.3, 11.4, 12.0, 12.1, 12.2, 12.3, 12.4, 13.0, 13.1, 13.2, 13.3, 13.4, 13.5, 13.6, 14.0, 14.1, 14.2, 14.3, 14.4, 14.5, 15.0, 15.2, 15.4, 16.0, 16.1, 16.2, 16.4
// MacCatalyst: 13.1, 13.2, 13.3, 13.4, 13.5, 14.2, 14.3, 14.4, 14.5, 15.0, 15.2, 15.4, 16.1, 16.2, 16.4
// macOS: 10.14, 10.15, 10.16, 11.0, 11.1, 11.2, 11.3, 12.0, 12.1, 12.3, 13.0, 13.1, 13.3
// tvOS: 10.0, 10.1, 10.2, 11.0, 11.1, 11.2, 11.3, 11.4, 12.0, 12.1, 12.2, 12.3, 12.4, 13.0, 13.2, 13.3, 13.4, 14.0, 14.2, 14.3, 14.4, 14.5, 15.0, 15.2, 15.4, 16.0, 16.1, 16.4
// Windows: 7.0, 8.0, 10.0.17763.0, 10.0.18362.0, 10.0.19041.0, 10.0.20348.0, 10.0.22000.0, 10.0.22621.0
//
// net7.0
// Android: 21.0, 22.0, 23.0, 24.0, 25.0, 26.0, 27.0, 28.0, 29.0, 30.0, 31.0, 32.0, 33.0
// iOS: 10.0, 10.1, 10.2, 10.3, 11.0, 11.1, 11.2, 11.3, 11.4, 12.0, 12.1, 12.2, 12.3, 12.4, 13.0, 13.1, 13.2, 13.3, 13.4, 13.5, 13.6, 14.0, 14.1, 14.2, 14.3, 14.4, 14.5, 15.0, 15.2, 15.4, 16.0, 16.1, 16.2, 16.4
// MacCatalyst: 13.1, 13.2, 13.3, 13.4, 13.5, 14.2, 14.3, 14.4, 14.5, 15.0, 15.2, 15.4, 16.1, 16.2, 16.4
// macOS: 10.14, 10.15, 10.16, 11.0, 11.1, 11.2, 11.3, 12.0, 12.1, 12.3, 13.0, 13.1, 13.3
// tvOS: 10.0, 10.1, 10.2, 11.0, 11.1, 11.2, 11.3, 11.4, 12.0, 12.1, 12.2, 12.3, 12.4, 13.0, 13.2, 13.3, 13.4, 14.0, 14.2, 14.3, 14.4, 14.5, 15.0, 15.2, 15.4, 16.0, 16.1, 16.4
// Windows: 7.0, 8.0, 10.0.17763.0, 10.0.18362.0, 10.0.19041.0, 10.0.20348.0, 10.0.22000.0, 10.0.22621.0
//
// net8.0
// Android: 21.0, 22.0, 23.0, 24.0, 25.0, 26.0, 27.0, 28.0, 29.0, 30.0, 31.0, 32.0, 33.0, 34.0
// iOS: 10.0, 10.1, 10.2, 10.3, 11.0, 11.1, 11.2, 11.3, 11.4, 12.0, 12.1, 12.2, 12.3, 12.4, 13.0, 13.1, 13.2, 13.3, 13.4, 13.5, 13.6, 14.0, 14.1, 14.2, 14.3, 14.4, 14.5, 15.0, 15.2, 15.4, 16.0, 16.1, 16.2, 16.4, 17.0, 17.2
// MacCatalyst: 13.1, 13.2, 13.3, 13.4, 13.5, 14.2, 14.3, 14.4, 14.5, 15.0, 15.2, 15.4, 16.1, 16.2, 16.4, 17.0, 17.2
// macOS: 10.14, 10.15, 10.16, 11.0, 11.1, 11.2, 11.3, 12.0, 12.1, 12.3, 13.0, 13.1, 13.3, 14.0, 14.2
// tvOS: 10.0, 10.1, 10.2, 11.0, 11.1, 11.2, 11.3, 11.4, 12.0, 12.1, 12.2, 12.3, 12.4, 13.0, 13.2, 13.3, 13.4, 14.0, 14.2, 14.3, 14.4, 14.5, 15.0, 15.2, 15.4, 16.0, 16.1, 16.4, 17.0, 17.2
// Windows: 7.0, 8.0, 10.0.17763.0, 10.0.18362.0, 10.0.19041.0, 10.0.20348.0, 10.0.22000.0, 10.0.22621.0
//
// Those are extracted via the the DumpPacks tool; however for that to work, you need to have
// the workloads installed (for each major.major of .NET separately) so I did this once so we
// don't have to do this again.
//
// This raises the question how we want to represent this here; right now we're expanding each
// framework into a set of paths to assemblies and index those, which means we're going to index
// the same set of assemblies again and again. Now, they aren't stored multiple times because the
// catalog builder de-duplicates assemblies based on their fingerprint, but we do spend time to
// build the XML files for those, which wasn't great when we had like 4 frameworks per major.minor
// version of .NET, but this entirely falls apart if we expand the list above. There are 342 version
// numbers; that means we're taking the existing 122 OS platforms and blow it up to about 450. So
// yeah, we'll have to think about indexing if we don't want that to take several hours more.

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
                    Platforms = ["windows", "windows10.0.17763"],
                    Kind = PackKind.Framework
                },
                new PackReference("Microsoft.Windows.SDK.NET.Ref")
                {
                    Version = "10.0.18362",
                    Platforms = ["windows", "windows10.0.18362"],
                    Kind = PackKind.Framework
                },
                new PackReference("Microsoft.Windows.SDK.NET.Ref")
                {
                    Version = "10.0.19041",
                    Platforms = ["windows", "windows10.0.19041"],
                    Kind = PackKind.Framework
                }
            ]
        },
        new FrameworkDefinition("net6.0")
        {
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
                    Platforms = ["windows", "windows10.0.17763"],
                    Kind = PackKind.Framework
                },
                new PackReference("Microsoft.Windows.SDK.NET.Ref")
                {
                    Version = "10.0.18362",
                    Platforms = ["windows", "windows10.0.18362"],
                    Kind = PackKind.Framework
                },
                new PackReference("Microsoft.Windows.SDK.NET.Ref")
                {
                    Version = "10.0.19041",
                    Platforms = ["windows", "windows10.0.19041"],
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
                    Platforms = ["windows", "windows10.0.17763"],
                    Kind = PackKind.Framework
                },
                new PackReference("Microsoft.Windows.SDK.NET.Ref")
                {
                    Version = "10.0.18362",
                    Platforms = ["windows", "windows10.0.18362"],
                    Kind = PackKind.Framework
                },
                new PackReference("Microsoft.Windows.SDK.NET.Ref")
                {
                    Version = "10.0.19041",
                    Platforms = ["windows", "windows10.0.19041"],
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
                    Platforms = ["windows", "windows10.0.17763"],
                    Kind = PackKind.Framework
                },
                new PackReference("Microsoft.Windows.SDK.NET.Ref")
                {
                    Version = "10.0.18362",
                    Platforms = ["windows", "windows10.0.18362"],
                    Kind = PackKind.Framework
                },
                new PackReference("Microsoft.Windows.SDK.NET.Ref")
                {
                    Version = "10.0.19041",
                    Platforms = ["windows", "windows10.0.19041"],
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