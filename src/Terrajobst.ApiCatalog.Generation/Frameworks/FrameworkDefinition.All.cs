namespace Terrajobst.ApiCatalog;

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
                    Version = "10.0.19041",
                    Platforms = ["windows"],
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
                    Version = "10.0.19041",
                    Platforms = ["windows"],
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
                    Version = "10.0.19041",
                    Platforms = ["windows"],
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
                    Version = "10.0.19041",
                    Platforms = ["windows"],
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