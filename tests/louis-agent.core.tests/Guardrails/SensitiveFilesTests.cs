using LouisAgent.Core.Guardrails;

namespace LouisAgent.Core.Tests.Guardrails;

// Files neither the agent's file tools nor the web app read or show.
public class SensitiveFilesTests
{
    [TestCase(".env", true)]
    [TestCase("config/.env.secrets", true)]
    [TestCase("certs/server.pem", true)]
    [TestCase("signing.key", true)]
    [TestCase("cert.pfx", true)]
    [TestCase("credentials.json", true)]
    [TestCase("Secrets.JSON", true)]
    [TestCase("appsettings.Development.json", true)]
    [TestCase(".ssh/id_ed25519", true)]
    [TestCase("config/.env.anthropic", true)]
    [TestCase(".env.example.local", true)]
    [TestCase("secrets.example", false)]
    // FX12: templates document the settings and are committed; they hold no secrets.
    [TestCase("config/.env.example", false)]
    [TestCase("config/.env.secrets.example", false)]
    [TestCase(".env.SAMPLE", false)]
    [TestCase(".env.template", false)]
    [TestCase("appsettings.json", false)]
    [TestCase("src/EnvironmentReader.cs", false)]
    [TestCase("README.md", false)]
    public void IsSensitive(string path, bool expected) =>
        Assert.That(SensitiveFiles.IsSensitive(path), Is.EqualTo(expected));
}
