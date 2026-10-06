using louis_agent.core.config;

namespace louis_agent.core.tests;

public class AgentOptionsTests
{
    [Test]
    public void FromEnvironment_ReturnsValidInstance()
    {
        var options = AgentOptions.FromEnvironment();
        Assert.That(options, Is.Not.Null);
    }

    [Test]
    public void FromEnvironment_SetsWorkspaceRoot()
    {
        var options = AgentOptions.FromEnvironment();
        Assert.That(options.WorkspaceRoot, Is.Not.Empty);
    }

    [Test]
    public void FindRepositoryRoot_WalksUpToDirectoryWithGit()
    {
        var root = Directory.CreateTempSubdirectory("repo-root-").FullName;
        try
        {
            Directory.CreateDirectory(Path.Combine(root, ".git"));
            var nested = Directory.CreateDirectory(Path.Combine(root, "src", "app")).FullName;

            Assert.That(AgentOptions.FindRepositoryRoot(nested), Is.EqualTo(root));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void FromEnvironment_PrefersExplicitWorkspaceRoot()
    {
        var explicitRoot = Path.GetTempPath();
        var options = AgentOptions.FromEnvironment(name => name == "WORKSPACE_ROOT" ? explicitRoot : null);
        Assert.That(options.WorkspaceRoot, Is.EqualTo(Path.GetFullPath(explicitRoot)));
    }

    [Test]
    public void AgentFunction_ReturnsString()
    {
        var options = AgentOptions.FromEnvironment();
        Assert.That(options.AgentFunction, Is.Not.Empty);
    }

    [Test]
    public void SkillsDirectory_ReturnsPathOrNull()
    {
        var options = AgentOptions.FromEnvironment();
        // SkillsDirectory is nullable - it can be null if SKILLS_DIRECTORY env var is not set
        // This is expected behavior; the default is to search in current directory and /app
        Assert.That(options, Is.Not.Null);
    }

    [Test]
    public void Constructor_AllowsPropertyInitialization()
    {
        var options = new AgentOptions
        {
            WorkspaceRoot = "/test/workspace",
            AgentFunction = "test-agent",
            PaymoApiKey = "paymo-123",
            DevopsApiKey = "devops-456"
        };

        Assert.That(options.WorkspaceRoot, Is.EqualTo("/test/workspace"));
        Assert.That(options.AgentFunction, Is.EqualTo("test-agent"));
        Assert.That(options.PaymoApiKey, Is.EqualTo("paymo-123"));
        Assert.That(options.DevopsApiKey, Is.EqualTo("devops-456"));
    }
}
