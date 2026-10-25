namespace LouisAgent.Core.Guardrails.Network;

using System.Net;
using System.Net.Sockets;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Guardrails.Messages;

/// <summary>
/// Server-side request forgery guard for fetching URLs: only public addresses, checked before every request (and every
/// redirect hop) and again at connect time, so the agent cannot be used to reach internal services or cloud metadata.
/// </summary>
internal static class SsrfGuard
{
    /// <summary>Throws <see cref="BlockedAddressException"/> unless every address the URI's host resolves to is public.</summary>
    internal static async Task EnsurePublicHostAsync(Uri uri, Func<string, CancellationToken, Task<IPAddress[]>> resolve)
    {
        if (uri.IsLoopback || uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            throw new BlockedAddressException(MessageText.Format(GuardrailToolMessage.FetchLocalAddress, new { uri.Host }));

        IPAddress[] addresses = IPAddress.TryParse(uri.Host.Trim('[', ']'), out var literal)
            ? [literal]
            : await resolve(uri.DnsSafeHost, CancellationToken.None);
        if (addresses.Length == 0) throw new HttpRequestException(MessageText.Format(GuardrailToolMessage.FetchUnresolvable, new { uri.Host }));
        if (addresses.FirstOrDefault(a => !IsPublicAddress(a)) is { } blocked)
            throw new BlockedAddressException(MessageText.Format(GuardrailToolMessage.FetchPrivateAddress, new { uri.Host, Address = blocked }));
    }

    /// <summary>
    /// True for globally routable unicast addresses. Rejects loopback, private (10/8, 172.16/12, 192.168/16),
    /// carrier-grade NAT, link-local (incl. 169.254.169.254 cloud metadata), multicast, reserved and IPv6 local ranges.
    /// </summary>
    internal static bool IsPublicAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any) ||
            address.Equals(IPAddress.None))
            return false;

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            byte[] b = address.GetAddressBytes();
            return !(b[0] == 0 || b[0] == 10 || b[0] == 127 ||
                     (b[0] == 100 && b[1] >= 64 && b[1] <= 127) ||
                     (b[0] == 169 && b[1] == 254) ||
                     (b[0] == 172 && b[1] >= 16 && b[1] <= 31) ||
                     (b[0] == 192 && b[1] == 168) ||
                     (b[0] == 192 && b[1] == 0 && b[2] == 0) ||
                     (b[0] == 198 && (b[1] == 18 || b[1] == 19)) ||
                     b[0] >= 224);
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            byte[] b = address.GetAddressBytes();
            return !(address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast ||
                     (b[0] & 0xFE) == 0xFC); // fc00::/7 unique local
        }

        return false;
    }

    /// <summary>
    /// A handler that re-checks the address actually connected to, so DNS changing between the check and the request
    /// cannot bypass <see cref="EnsurePublicHostAsync"/>. It doesn't follow redirects: the caller checks each hop.
    /// </summary>
    internal static SocketsHttpHandler CreateGuardedHandler() => new()
    {
        AllowAutoRedirect = false,
        AutomaticDecompression = DecompressionMethods.All,
        ConnectTimeout = TimeSpan.FromSeconds(10),
        ConnectCallback = async (context, ct) =>
        {
            IPAddress[] addresses = IPAddress.TryParse(context.DnsEndPoint.Host, out var literal)
                ? [literal]
                : await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, ct);
            var allowed = addresses.Where(IsPublicAddress).ToArray();
            if (allowed.Length == 0)
                throw new BlockedAddressException(MessageText.Format(GuardrailToolMessage.ConnectPrivateAddress, new { context.DnsEndPoint.Host }));

            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            try
            {
                await socket.ConnectAsync(allowed, context.DnsEndPoint.Port, ct);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        },
    };
}