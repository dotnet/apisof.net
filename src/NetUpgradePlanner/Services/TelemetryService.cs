using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;

using NetUpgradePlanner.Analysis;

namespace NetUpgradePlanner.Services;

internal sealed class TelemetryService : BackgroundService
{
    private const string SendTelemetrySettingName = "SendTelemetry";

    private readonly OfflineDetectionService _offlineDetectionService;
    private readonly WorkspaceService _workspaceService;
    private readonly Worker _worker = new Worker();

    public TelemetryService(OfflineDetectionService offlineDetectionService, WorkspaceService workspaceService)
    {
        _offlineDetectionService = offlineDetectionService;
        _workspaceService = workspaceService;
        _workspaceService.Changed += WorkspaceService_Changed;
        _worker.IsEnabled = GetTelemetrySetting();
    }

    public bool IsConfigured()
    {
        return _offlineDetectionService.IsOfflineInstallation ||
               SettingsService.IsConfigured(SendTelemetrySettingName);
    }

    private bool GetTelemetrySetting()
    {
#if DEBUG
        // Let's not pollute the telemetry when debugging stuff.
        return false;
#else
        if (_offlineDetectionService.IsOfflineInstallation)
            return false;

        return SettingsService.LoadValue(SendTelemetrySettingName, true);
#endif
    }

    private static void SetTelemetrySetting(bool enabled)
    {
        SettingsService.StoreValue(SendTelemetrySettingName, enabled);
    }

    public bool IsEnabled
    {
        get
        {
            return _worker.IsEnabled;
        }
        set
        {
            _worker.IsEnabled = value;
            SetTelemetrySetting(value);
        }
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(async () =>
        {
            try
            {
                await _worker.RunAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Telemetry processing stopped.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Telemetry processing failed: {ex}");
            }
        }, stoppingToken);
    }

    private void WorkspaceService_Changed(object? sender, EventArgs e)
    {
        foreach (var entry in _workspaceService.Current.AssemblySet.Entries)
            _worker.Enqueue(entry);
    }

    private sealed class Worker
    {
        private readonly ConcurrentQueue<AssemblySetEntry> _entryQueue = new ConcurrentQueue<AssemblySetEntry>();
        private readonly AutoResetEvent _dataAvailable = new(false);
        private readonly HashSet<string> _processedFingerprints = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HttpClient _client = new HttpClient();

        public bool IsEnabled { get; set; }

        public void Enqueue(AssemblySetEntry entry)
        {
            _entryQueue.Enqueue(entry);
            _dataAvailable.Set();
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            var waitHandles = new[]
            {
                _dataAvailable,
                cancellationToken.WaitHandle
            };

            while (true)
            {
                Debug.WriteLine($"Waiting for telemetry data...");

                WaitHandle.WaitAny(waitHandles);
                cancellationToken.ThrowIfCancellationRequested();

                while (_entryQueue.TryDequeue(out var entry))
                    await ProcessAsync(entry);
            }
        }

        private async Task ProcessAsync(AssemblySetEntry entry)
        {
            if (!IsEnabled || entry.UsedApis.Count == 0 || !_processedFingerprints.Add(entry.Fingerprint))
                return;

            var uri = new Uri($"https://functions.apisof.net/store-telemetry?fingerprint={entry.Fingerprint}");
            var body = string.Join(Environment.NewLine, entry.UsedApis);
            var stringContent = new StringContent(body);
            try
            {
                var response = await _client.PostAsync(uri, stringContent);
                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"Failed to send telemetry for {entry.Fingerprint}: {response.StatusCode}");
                }
                else
                {
                    Debug.WriteLine($"Sent telemetry for {entry.Fingerprint}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to send telemetry: {ex}");
                IsEnabled = false;
            }
        }
    }
}
