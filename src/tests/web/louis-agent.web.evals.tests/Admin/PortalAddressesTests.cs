namespace LouisAgent.Web.Evals.Tests.Admin;

/// <summary>
/// The portal's own addresses are written in one place (F53.2 step 1, ADR 0065): no page or component spells a run's
/// address itself, and the retired addresses (<c>admin/benchmarks</c>, the Afrikaans <c>admin/maatstawwe</c>) are
/// gone for good (ADR 0049). The API's own routes (<c>v1/admin/benchmarks/…</c>) are not web addresses and stay.
/// </summary>
/// <remarks>
/// A ratchet, as <c>PortalReadsJsonTests</c>: a file that writes a retired address fails, with no list to grow.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0065-the-view-is-in-the-address.md">ADR 0065: the view is in the address</seealso>
public class PortalAddressesTests
{
    /// <summary>The addresses no file may write any more.</summary>
    private static readonly string[] Retired = ["admin/benchmarks", "admin/maatstawwe"];

    /// <summary>No page or component writes a retired address; the API's routes don't count.</summary>
    [Test]
    public void NoPortalFile_WritesARetiredAddress()
    {
        var writing = Directory.EnumerateFiles(Path.Combine(TestPaths.RepoRoot, "src/web/louis-agent.web"), "*",
                SearchOption.AllDirectories)
            .Where(file => file.EndsWith(".cs", StringComparison.Ordinal) ||
                           file.EndsWith(".razor", StringComparison.Ordinal))
            .Where(file => Retired.Any(address =>
                File.ReadAllText(file).Replace("v1/" + address, "", StringComparison.Ordinal)
                    .Contains(address, StringComparison.Ordinal)))
            .Select(file => Path.GetRelativePath(TestPaths.RepoRoot, file).Replace('\\', '/'))
            .ToList();

        Assert.That(writing, Is.Empty, "write the address through RunAddress");
    }
}
