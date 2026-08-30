using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace DisplayOnWeb.Host.Networking;

public sealed class LanAddressService
{
    public IReadOnlyList<string> GetPrivateLanAddresses() => NetworkInterface.GetAllNetworkInterfaces()
        .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType is not NetworkInterfaceType.Loopback and not NetworkInterfaceType.Tunnel)
        .SelectMany(n => n.GetIPProperties().UnicastAddresses)
        .Select(a => a.Address)
        .Where(a => a.AddressFamily == AddressFamily.InterNetwork && IsPrivate(a))
        .Select(a => a.ToString()).Distinct().ToArray();

    public string GetDisplayUrl(int port)
    {
        var address = GetPrivateLanAddresses().FirstOrDefault() ?? "127.0.0.1";
        return $"http://{address}:{port}";
    }

    public (string Address, string Network) GetPrimaryLanEndpoint()
    {
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces()
                     .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType is not NetworkInterfaceType.Loopback and not NetworkInterfaceType.Tunnel))
        {
            foreach (var entry in nic.GetIPProperties().UnicastAddresses)
            {
                if (entry.Address.AddressFamily != AddressFamily.InterNetwork || !IsPrivate(entry.Address)) continue;
                var prefix = entry.PrefixLength;
                var bytes = entry.Address.GetAddressBytes();
                var value = BitConverter.ToUInt32(bytes.Reverse().ToArray());
                var mask = prefix == 0 ? 0u : uint.MaxValue << (32 - prefix);
                var networkBytes = BitConverter.GetBytes(value & mask).Reverse().ToArray();
                return (entry.Address.ToString(), $"{new IPAddress(networkBytes)}/{prefix}");
            }
        }

        return ("127.0.0.1", "127.0.0.1/32");
    }

    private static bool IsPrivate(IPAddress address)
    {
        var b = address.GetAddressBytes();
        return b[0] == 10 || (b[0] == 172 && b[1] is >= 16 and <= 31) || (b[0] == 192 && b[1] == 168);
    }
}
