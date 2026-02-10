namespace PortScanner.Models;

public class ScanConfiguration
{
    public string Target { get; set; } = "";
    public string PortSpec { get; set; } = "21,22,23,25,80,443,445,3389,8080";
    public ScanProtocol Protocol { get; set; } = ScanProtocol.TCP;
    public int MaxThreads { get; set; } = 100;
    public int Timeout { get; set; } = 1000;
}
