using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using PortScanner.Models;

namespace PortScanner.Services;

public class PortScannerService
{
    private readonly ConcurrentDictionary<string, string> _dnsCache = new();

    public async Task<List<ScanResult>> ScanAsync(
        ScanConfiguration config,
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var results = new ConcurrentBag<ScanResult>();
        var targets = ExpandTargets(config.Target);
        var ports = ParsePorts(config.PortSpec);
        var protocols = GetProtocols(config.Protocol);

        long totalTasks = (long)targets.Count * ports.Count * protocols.Count;
        long completedTasks = 0;
        int openCount = 0;
        int closedCount = 0;
        int filteredCount = 0;

        IEnumerable<(string Target, int Port, string Protocol)> GetScanItems()
        {
            foreach (var target in targets)
                foreach (var port in ports)
                    foreach (var protocol in protocols)
                        yield return (target, port, protocol);
        }

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = config.MaxThreads,
            CancellationToken = cancellationToken
        };

        try
        {
            await Parallel.ForEachAsync(GetScanItems(), options, async (item, ct) =>
            {
                var (target, port, protocol) = item;

                var result = protocol == "TCP"
                    ? await ScanTcpPortAsync(target, port, config.Timeout)
                    : await ScanUdpPortAsync(target, port, config.Timeout * 2);

                if (result.Status == PortStatus.Open || result.Status == PortStatus.OpenFiltered)
                    result.Hostname = await ResolveHostnameAsync(target);

                results.Add(result);

                long completed = Interlocked.Increment(ref completedTasks);

                switch (result.Status)
                {
                    case PortStatus.Open:
                    case PortStatus.OpenFiltered:
                        Interlocked.Increment(ref openCount);
                        break;
                    case PortStatus.Closed:
                        Interlocked.Increment(ref closedCount);
                        break;
                    case PortStatus.Filtered:
                        Interlocked.Increment(ref filteredCount);
                        break;
                }

                if (progress != null && (completed % 10 == 0 || completed == totalTasks))
                {
                    progress.Report(new ScanProgress
                    {
                        Completed = (int)completed,
                        Total = (int)totalTasks,
                        OpenCount = openCount,
                        ClosedCount = closedCount,
                        FilteredCount = filteredCount
                    });
                }
            });
        }
        catch (OperationCanceledException) { }

        return results.ToList();
    }

    private async Task<string> ResolveHostnameAsync(string target)
    {
        if (_dnsCache.TryGetValue(target, out var cached))
            return cached;

        try
        {
            var entry = await Dns.GetHostEntryAsync(target);
            var hostname = entry.HostName;

            if (hostname == target)
                hostname = "";

            _dnsCache.TryAdd(target, hostname);
            return hostname;
        }
        catch
        {
            _dnsCache.TryAdd(target, "");
            return "";
        }
    }

    private async Task<ScanResult> ScanTcpPortAsync(string target, int port, int timeout)
    {
        var result = new ScanResult
        {
            Target = target,
            Port = port,
            Protocol = "TCP",
            Status = PortStatus.Closed,
            Service = ServiceResolver.GetServiceName(port),
            Timestamp = DateTime.Now
        };

        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(target, port);

            if (await Task.WhenAny(connectTask, Task.Delay(timeout)) == connectTask)
            {
                if (client.Connected)
                    result.Status = PortStatus.Open;
            }
            else
            {
                result.Status = PortStatus.Filtered;
            }
        }
        catch (SocketException ex)
        {
            result.Status = ex.SocketErrorCode switch
            {
                SocketError.ConnectionRefused => PortStatus.Closed,
                SocketError.HostUnreachable => PortStatus.Filtered,
                SocketError.TimedOut => PortStatus.Filtered,
                _ => PortStatus.Filtered
            };
        }
        catch
        {
            result.Status = PortStatus.Error;
        }

        return result;
    }

    private async Task<ScanResult> ScanUdpPortAsync(string target, int port, int timeout)
    {
        var result = new ScanResult
        {
            Target = target,
            Port = port,
            Protocol = "UDP",
            Status = PortStatus.OpenFiltered,
            Service = ServiceResolver.GetServiceName(port),
            Timestamp = DateTime.Now
        };

        try
        {
            using var client = new UdpClient();
            client.Client.ReceiveTimeout = timeout;
            client.Client.SendTimeout = timeout;

            await client.SendAsync(new byte[] { 0x00 }, 1, target, port);

            var receiveTask = client.ReceiveAsync();
            if (await Task.WhenAny(receiveTask, Task.Delay(timeout)) == receiveTask)
                result.Status = PortStatus.Open;
        }
        catch (SocketException ex)
        {
            result.Status = ex.SocketErrorCode switch
            {
                SocketError.ConnectionReset => PortStatus.Closed,
                SocketError.TimedOut => PortStatus.OpenFiltered,
                _ => PortStatus.OpenFiltered
            };
        }
        catch
        {
            result.Status = PortStatus.Error;
        }

        return result;
    }

    public List<string> ExpandTargets(string target)
    {
        var targets = new List<string>();
        var parts = target.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var part in parts)
        {
            if (part.Contains('/'))
                targets.AddRange(ExpandCidr(part));
            else if (part.Contains('-') && part.LastIndexOf('.') < part.LastIndexOf('-'))
                targets.AddRange(ExpandRange(part));
            else
                targets.Add(part);
        }

        return targets.Distinct().ToList();
    }

    private List<string> ExpandCidr(string cidr)
    {
        var targets = new List<string>();

        try
        {
            var parts = cidr.Split('/');
            var baseIp = IPAddress.Parse(parts[0]);
            var prefixLength = int.Parse(parts[1]);

            var ipBytes = baseIp.GetAddressBytes();
            Array.Reverse(ipBytes);
            var ipInt = BitConverter.ToUInt32(ipBytes, 0);

            var hostBits = 32 - prefixLength;
            var networkMask = uint.MaxValue << hostBits;
            var networkInt = ipInt & networkMask;
            var broadcastInt = networkInt | ~networkMask;

            if (prefixLength >= 31)
            {
                for (uint i = networkInt; i <= broadcastInt; i++)
                {
                    var bytes = BitConverter.GetBytes(i);
                    Array.Reverse(bytes);
                    targets.Add(new IPAddress(bytes).ToString());
                }
            }
            else
            {
                for (uint i = networkInt + 1; i < broadcastInt; i++)
                {
                    var bytes = BitConverter.GetBytes(i);
                    Array.Reverse(bytes);
                    targets.Add(new IPAddress(bytes).ToString());
                }
            }
        }
        catch { }

        return targets;
    }

    private List<string> ExpandRange(string range)
    {
        var targets = new List<string>();

        try
        {
            var lastDot = range.LastIndexOf('.');
            var prefix = range[..lastDot];
            var rangePart = range[(lastDot + 1)..];
            var rangeParts = rangePart.Split('-');

            var start = int.Parse(rangeParts[0]);
            var end = int.Parse(rangeParts[1]);

            for (int i = start; i <= end; i++)
                targets.Add($"{prefix}.{i}");
        }
        catch { }

        return targets;
    }

    public List<int> ParsePorts(string portSpec)
    {
        var ports = new HashSet<int>();

        foreach (var part in portSpec.Split(','))
        {
            var trimmed = part.Trim();

            if (trimmed.Contains('-'))
            {
                var rangeParts = trimmed.Split('-');
                if (int.TryParse(rangeParts[0], out var start) && int.TryParse(rangeParts[1], out var end))
                {
                    for (int i = start; i <= end; i++)
                    {
                        if (i >= 1 && i <= 65535)
                            ports.Add(i);
                    }
                }
            }
            else if (int.TryParse(trimmed, out int port) && port >= 1 && port <= 65535)
            {
                ports.Add(port);
            }
        }

        return ports.OrderBy(p => p).ToList();
    }

    private List<string> GetProtocols(ScanProtocol protocol)
    {
        return protocol switch
        {
            ScanProtocol.TCP => ["TCP"],
            ScanProtocol.UDP => ["UDP"],
            ScanProtocol.Both => ["TCP", "UDP"],
            _ => ["TCP"]
        };
    }
}
