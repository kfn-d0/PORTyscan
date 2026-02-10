namespace PortScanner.Models;

public enum ScanProtocol
{
    TCP,
    UDP,
    Both
}

public enum PortStatus
{
    Open,
    Closed,
    Filtered,
    OpenFiltered,
    Error
}
