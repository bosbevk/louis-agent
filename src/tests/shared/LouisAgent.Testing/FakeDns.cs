using System.Net;

namespace LouisAgent.Testing;

/// <summary>A DNS answer for tests that must get past the SSRF guard without leaving the machine.</summary>
public static class FakeDns
{
    /// <summary>Every host resolves to one public address.</summary>
    public static Task<IPAddress[]> Public(string host, CancellationToken _) =>
        Task.FromResult(new[] { IPAddress.Parse("93.184.216.34") });
}
