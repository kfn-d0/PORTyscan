namespace PortScanner.Models;

public class ScanResult
{
    public string Target { get; set; } = "";
    public string Hostname { get; set; } = "";
    public int Port { get; set; }
    public string Protocol { get; set; } = "";
    public PortStatus Status { get; set; }
    public string Service { get; set; } = "";
    public DateTime Timestamp { get; set; }
}

public class ScanProgress
{
    public int Completed { get; set; }
    public int Total { get; set; }
    public int OpenCount { get; set; }
    public int ClosedCount { get; set; }
    public int FilteredCount { get; set; }
    public double Percentage => Total > 0 ? (double)Completed / Total * 100 : 0;
}

