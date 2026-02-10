namespace PortScanner.Services;

public static class ServiceResolver
{
    private static readonly Dictionary<int, string> Services = new()
    {
        { 20, "FTP-Data" },
        { 21, "FTP" },
        { 22, "SSH" },
        { 23, "Telnet" },
        { 25, "SMTP" },
        { 53, "DNS" },
        { 67, "DHCP-Server" },
        { 68, "DHCP-Client" },
        { 69, "TFTP" },
        { 80, "HTTP" },
        { 110, "POP3" },
        { 111, "RPC" },
        { 119, "NNTP" },
        { 123, "NTP" },
        { 135, "MS-RPC" },
        { 137, "NetBIOS-NS" },
        { 138, "NetBIOS-DGM" },
        { 139, "NetBIOS-SSN" },
        { 143, "IMAP" },
        { 161, "SNMP" },
        { 162, "SNMP-Trap" },
        { 389, "LDAP" },
        { 443, "HTTPS" },
        { 445, "SMB" },
        { 465, "SMTPS" },
        { 500, "IKE" },
        { 514, "Syslog" },
        { 515, "LPD" },
        { 520, "RIP" },
        { 587, "SMTP-Submission" },
        { 636, "LDAPS" },
        { 993, "IMAPS" },
        { 995, "POP3S" },
        { 1080, "SOCKS" },
        { 1433, "MSSQL" },
        { 1434, "MSSQL-UDP" },
        { 1521, "Oracle" },
        { 1723, "PPTP" },
        { 2049, "NFS" },
        { 3306, "MySQL" },
        { 3389, "RDP" },
        { 3690, "SVN" },
        { 4443, "HTTPS-Alt" },
        { 5060, "SIP" },
        { 5061, "SIPS" },
        { 5432, "PostgreSQL" },
        { 5433, "PostgreSQL-Alt" },
        { 5900, "VNC" },
        { 5985, "WinRM-HTTP" },
        { 5986, "WinRM-HTTPS" },
        { 6379, "Redis" },
        { 8000, "HTTP-Alt" },
        { 8080, "HTTP-Proxy" },
        { 8443, "HTTPS-Alt" },
        { 8888, "HTTP-Alt" },
        { 9000, "HTTP-Alt" },
        { 9090, "Web-Console" },
        { 9200, "Elasticsearch" },
        { 11211, "Memcached" },
        { 27017, "MongoDB" }
    };

    public static string GetServiceName(int port)
    {
        return Services.TryGetValue(port, out var service) ? service : "Unknown";
    }
}
