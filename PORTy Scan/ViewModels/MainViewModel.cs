using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using PortScanner.Models;
using PortScanner.Services;

namespace PortScanner.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly PortScannerService _scannerService = new();
    private CancellationTokenSource? _cancellationTokenSource;

    [ObservableProperty]
    private string _target = "";

    [ObservableProperty]
    private string _customPorts = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCustomPortPreset))]
    private int _selectedPortPresetIndex = 0;

    public bool IsCustomPortPreset => SelectedPortPresetIndex == 4;

    [ObservableProperty]
    private int _selectedProtocolIndex = 0;

    [ObservableProperty]
    private int _threads = 100;

    [ObservableProperty]
    private int _timeout = 1000;

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private string _progressText = "Ready";

    [ObservableProperty]
    private int _openCount;

    [ObservableProperty]
    private int _closedCount;

    [ObservableProperty]
    private int _filteredCount;

    [ObservableProperty]
    private int _totalScanned;

    [ObservableProperty]
    private string _scanDuration = "00:00:00";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasResults))]
    private ObservableCollection<ScanResultViewModel> _results = new();

    public bool HasResults => Results.Count > 0;

    public List<string> PortPresets { get; } =
    [
        "Common (Top 20)",
        "Web Ports",
        "Database Ports",
        "All Common (Top 100)",
        "Custom"
    ];

    public List<string> Protocols { get; } = ["TCP (Recomendado, Padrão)", "UDP (Não recomendado)", "Both"];

    private readonly Dictionary<int, string> _portPresetValues = new()
    {
        { 0, "21,22,23,25,53,80,110,135,139,143,443,445,993,995,1433,3306,3389,5432,8080,8443" },
        { 1, "80,443,8080,8443,8000,8888,9000,9090" },
        { 2, "1433,1434,3306,5432,5433,6379,27017,9200,11211" },
        { 3, "21,22,23,25,53,67,68,69,80,110,111,119,123,135,137,138,139,143,161,162,389,443,445,465,500,514,515,520,587,636,993,995,1080,1433,1434,1521,1723,2049,3306,3389,3690,4443,5060,5061,5432,5900,5985,5986,6379,8000,8080,8443,9000,9090,9200,11211,27017" },
        { 4, "" }
    };

    [RelayCommand(CanExecute = nameof(CanStartScan))]
    private async Task StartScanAsync()
    {
        if (string.IsNullOrWhiteSpace(Target))
            return;

        ClearResults();

        IsScanning = true;
        Progress = 0;
        ProgressText = "Starting scan...";

        _cancellationTokenSource = new CancellationTokenSource();
        var startTime = DateTime.Now;

        var config = new ScanConfiguration
        {
            Target = Target,
            PortSpec = SelectedPortPresetIndex == 4 ? CustomPorts : _portPresetValues[SelectedPortPresetIndex],
            Protocol = (ScanProtocol)SelectedProtocolIndex,
            MaxThreads = Threads,
            Timeout = Timeout
        };

        var progressReporter = new Progress<ScanProgress>(p =>
        {
            Progress = p.Percentage;
            OpenCount = p.OpenCount;
            ClosedCount = p.ClosedCount;
            FilteredCount = p.FilteredCount;
            TotalScanned = p.Completed;
            ProgressText = $"Scanning: {p.Completed}/{p.Total} ({p.Percentage:F1}%)";
            ScanDuration = (DateTime.Now - startTime).ToString(@"hh\:mm\:ss");
        });

        try
        {
            var results = await _scannerService.ScanAsync(config, progressReporter, _cancellationTokenSource.Token);

            var filtered = results
                .Where(r => r.Status == PortStatus.Open || r.Status == PortStatus.OpenFiltered)
                .OrderBy(r => r.Target).ThenBy(r => r.Port)
                .Select(r => new ScanResultViewModel(r));

            foreach (var item in filtered)
            {
                if (!Results.Any(r => r.Target == item.Target && r.Port == item.Port))
                    Results.Add(item);
            }

            if (_cancellationTokenSource.IsCancellationRequested)
                ProgressText = $"Scan cancelled. Found {OpenCount} open ports so far.";
            else
                ProgressText = $"Scan complete! Found {OpenCount} open ports";
        }
        catch (Exception ex)
        {
            ProgressText = $"Error: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
            ScanDuration = (DateTime.Now - startTime).ToString(@"hh\:mm\:ss");
        }
    }

    private bool CanStartScan() => !IsScanning && !string.IsNullOrWhiteSpace(Target);

    [RelayCommand(CanExecute = nameof(CanStopScan))]
    private void StopScan()
    {
        _cancellationTokenSource?.Cancel();
        ProgressText = "Stopping...";
    }

    private bool CanStopScan() => IsScanning;

    [RelayCommand]
    private async Task ExportCsvAsync()
    {
        if (!HasResults) return;

        var fileName = $"PortScan_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);

        var csv = new StringBuilder();
        csv.AppendLine("Host,Hostname,Port,Protocol,Status,Service,Timestamp");

        foreach (var result in Results)
        {
            csv.AppendLine($"{result.Target},{result.Hostname},{result.Port},{result.Protocol},{result.Status},{result.Service},{result.Timestamp:yyyy-MM-dd HH:mm:ss}");
        }

        await File.WriteAllTextAsync(filePath, csv.ToString());
        ProgressText = $"Exported to {fileName}";
    }

    [RelayCommand]
    private async Task ExportHtmlAsync()
    {
        if (!HasResults) return;

        var fileName = $"PortScan_{DateTime.Now:yyyyMMdd_HHmmss}.html";
        var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);

        var html = GenerateHtmlReport();
        await File.WriteAllTextAsync(filePath, html);
        ProgressText = $"Exported to {fileName}";
    }

    [RelayCommand]
    private void ClearResults()
    {
        Results = new();
        OpenCount = 0;
        ClosedCount = 0;
        FilteredCount = 0;
        TotalScanned = 0;
        Progress = 0;
        ProgressText = "Ready";
        ScanDuration = "00:00:00";
    }

    partial void OnIsScanningChanged(bool value)
    {
        StartScanCommand.NotifyCanExecuteChanged();
        StopScanCommand.NotifyCanExecuteChanged();
    }

    partial void OnTargetChanged(string value)
    {
        StartScanCommand.NotifyCanExecuteChanged();
    }

    private string GenerateHtmlReport()
    {
        var html = new StringBuilder();
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html lang='en'>");
        html.AppendLine("<head>");
        html.AppendLine("  <meta charset='UTF-8'>");
        html.AppendLine("  <meta name='viewport' content='width=device-width, initial-scale=1.0'>");
        html.AppendLine($"  <title>Port Scan Report - {DateTime.Now:yyyy-MM-dd HH:mm}</title>");
        html.AppendLine("  <style>");
        html.AppendLine(@"
            :root {
                --bg-primary: #0d1117;
                --bg-secondary: #161b22;
                --bg-card: #21262d;
                --text-primary: #c9d1d9;
                --text-secondary: #8b949e;
                --accent-green: #3fb950;
                --accent-red: #f85149;
                --accent-yellow: #d29922;
                --accent-blue: #58a6ff;
                --border-color: #30363d;
            }
            * { margin: 0; padding: 0; box-sizing: border-box; }
            body {
                font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Helvetica, Arial, sans-serif;
                background: var(--bg-primary);
                color: var(--text-primary);
                line-height: 1.6;
                padding: 40px 20px;
            }
            .container { max-width: 1200px; margin: 0 auto; }
            header {
                text-align: center;
                padding: 40px 0;
                border-bottom: 1px solid var(--border-color);
                margin-bottom: 40px;
            }
            header h1 {
                font-size: 2.5em;
                background: linear-gradient(135deg, var(--accent-blue), var(--accent-green));
                -webkit-background-clip: text;
                -webkit-text-fill-color: transparent;
                margin-bottom: 10px;
            }
            .stats-grid {
                display: grid;
                grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
                gap: 20px;
                margin-bottom: 40px;
            }
            .stat-card {
                background: var(--bg-secondary);
                border-radius: 12px;
                padding: 24px;
                text-align: center;
                border: 1px solid var(--border-color);
            }
            .stat-card h3 { font-size: 2.5em; margin-bottom: 8px; }
            .stat-card p { color: var(--text-secondary); text-transform: uppercase; font-size: 0.85em; letter-spacing: 1px; }
            .stat-card.open h3 { color: var(--accent-green); }
            .stat-card.closed h3 { color: var(--accent-red); }
            .stat-card.filtered h3 { color: var(--accent-yellow); }
            .stat-card.total h3 { color: var(--accent-blue); }
            .section {
                background: var(--bg-secondary);
                border-radius: 12px;
                padding: 24px;
                margin-bottom: 24px;
                border: 1px solid var(--border-color);
            }
            .section h2 {
                margin-bottom: 20px;
                padding-bottom: 12px;
                border-bottom: 1px solid var(--border-color);
            }
            table { width: 100%; border-collapse: collapse; }
            th, td { padding: 12px; text-align: left; border-bottom: 1px solid var(--border-color); }
            th { background: var(--bg-card); font-weight: 600; }
            tr:hover { background: var(--bg-card); }
            .status-open { color: var(--accent-green); font-weight: 600; }
            .status-filtered { color: var(--accent-yellow); }
            .hostname { color: var(--accent-blue); font-size: 0.85em; }
            footer { text-align: center; padding: 40px; color: var(--text-secondary); }
        ");
        html.AppendLine("  </style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine("  <div class='container'>");
        html.AppendLine("    <header>");
        html.AppendLine("      <h1>PORTy Scan Report</h1>");
        html.AppendLine($"      <p>Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");
        html.AppendLine($"      <p>Target: {Target}</p>");
        html.AppendLine("    </header>");

        html.AppendLine("    <div class='stats-grid'>");
        html.AppendLine($"      <div class='stat-card open'><h3>{OpenCount}</h3><p>Open Ports</p></div>");
        html.AppendLine($"      <div class='stat-card closed'><h3>{ClosedCount}</h3><p>Closed Ports</p></div>");
        html.AppendLine($"      <div class='stat-card filtered'><h3>{FilteredCount}</h3><p>Filtered</p></div>");
        html.AppendLine($"      <div class='stat-card total'><h3>{TotalScanned}</h3><p>Total Scanned</p></div>");
        html.AppendLine("    </div>");

        if (Results.Count > 0)
        {
            html.AppendLine("    <div class='section'>");
            html.AppendLine("      <h2>Open Ports</h2>");
            html.AppendLine("      <table>");
            html.AppendLine("        <thead><tr><th>Host</th><th>Hostname</th><th>Port</th><th>Protocol</th><th>Status</th><th>Service</th></tr></thead>");
            html.AppendLine("        <tbody>");

            foreach (var result in Results)
            {
                var statusClass = result.Status == "Open" ? "status-open" : "status-filtered";
                var hostnameDisplay = string.IsNullOrEmpty(result.Hostname) ? "—" : result.Hostname;
                html.AppendLine($"          <tr><td>{result.Target}</td><td class='hostname'>{hostnameDisplay}</td><td>{result.Port}</td><td>{result.Protocol}</td><td class='{statusClass}'>{result.Status}</td><td>{result.Service}</td></tr>");
            }

            html.AppendLine("        </tbody>");
            html.AppendLine("      </table>");
            html.AppendLine("    </div>");
        }

        html.AppendLine("    <footer>");
        html.AppendLine("      <p>Generated by PORTy Scan</p>");
        html.AppendLine("    </footer>");
        html.AppendLine("  </div>");
        html.AppendLine("</body>");
        html.AppendLine("</html>");

        return html.ToString();
    }
}

public partial class ScanResultViewModel : ObservableObject
{
    public string Target { get; }
    public string Hostname { get; }
    public int Port { get; }
    public string Protocol { get; }
    public string Status { get; }
    public string Service { get; }
    public DateTime Timestamp { get; }

    public ScanResultViewModel(ScanResult result)
    {
        Target = result.Target;
        Hostname = result.Hostname;
        Port = result.Port;
        Protocol = result.Protocol;
        Status = result.Status.ToString();
        Service = result.Service;
        Timestamp = result.Timestamp;
    }
}
