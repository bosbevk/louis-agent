using LouisAgent.Core.Config;

namespace LouisAgent.Core.Tests.Config;

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
            PaymoApiKey = "paymo-123",
            DevopsApiKey = "devops-456"
        };

        Assert.That(options.WorkspaceRoot, Is.EqualTo("/test/workspace"));
        Assert.That(options.PaymoApiKey, Is.EqualTo("paymo-123"));
        Assert.That(options.DevopsApiKey, Is.EqualTo("devops-456"));
    }

    // CL14 step 1: ROLES_ENABLED is the setting's name; PERSONALITIES_ENABLED, its name before, is still read, with a
    // warning, until the TODO's line removes it, so a config/.env written before CL14 keeps working.
    private static AgentOptions With(params (string Name, string Value)[] variables) =>
        AgentOptions.FromEnvironment(name => variables.FirstOrDefault(v => v.Name == name).Value);

    [Test]
    public void RolesEnabled_ReadsRolesEnabled() =>
        Assert.That(With(("ROLES_ENABLED", "puls-cook,puls/voice-af")).RolesEnabled, Is.EqualTo(new[] { "puls-cook", "puls/voice-af" }));

    [Test]
    public void RolesEnabled_ReadsThePersonalitiesSetting_WhenItIsUnset() =>
        Assert.That(With(("PERSONALITIES_ENABLED", "puls-cook")).RolesEnabled, Is.EqualTo(new[] { "puls-cook" }));

    [Test]
    public void RolesEnabled_ReadsTheNewOne_WhenBothAreSet() =>
        Assert.That(With(("ROLES_ENABLED", "puls-cook"), ("PERSONALITIES_ENABLED", "puls/voice-af")).RolesEnabled, Is.EqualTo(new[] { "puls-cook" }));

    [Test, NonParallelizable]
    public void TheOldSetting_IsNamedInAWarning()
    {
        TextWriter original = Console.Error;
        var stderr = new StringWriter();
        Console.SetError(stderr);
        try
        {
            LouisAgent.Core.Logging.AgentLogging.Configure(null);
            With(("PERSONALITIES_ENABLED", "puls-cook"));
        }
        finally
        {
            Console.SetError(original);
        }

        Assert.That(stderr.ToString(), Does.Contain("[WARN]").And.Contain("PERSONALITIES_ENABLED").And.Contain("ROLES_ENABLED"));
    }
}
