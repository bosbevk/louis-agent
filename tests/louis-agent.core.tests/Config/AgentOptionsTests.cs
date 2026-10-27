using LouisAgent.Testing;
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
    [Test]
    public void BrowserUserAgent_IsRead_AndAnEmptyOneIsNone()
    {
        // FX21: an identity names itself to the sites its browser reads for a user; unset or blank, Chrome's own is sent.
        Assert.That(
            With(("BROWSER_USER_AGENT", "Puls/1.0 (+https://example.test; reads a page on a user's behalf)"))
                .BrowserUserAgent,
            Is.EqualTo("Puls/1.0 (+https://example.test; reads a page on a user's behalf)"));
        Assert.That(With(("BROWSER_USER_AGENT", "   ")).BrowserUserAgent, Is.Null);
        Assert.That(With().BrowserUserAgent, Is.Null);
    }

    [Test]
    public void TheReplaySettings_AreRead_TrimmedAndNullWhenBlank()
    {
        // F18 step 4a: WEB_REPLAY is the text SessionReplay.DefaultMode parses; REPLAY_DIRECTORY decides whether recordings are kept.
        var set = With(("WEB_REPLAY", " record "), ("REPLAY_DIRECTORY", " /data/replay "));
        Assert.That((set.WebReplay, set.ReplayDirectory, set.KeepsRecordings),
            Is.EqualTo(("record", "/data/replay", true)));

        var blank = With(("WEB_REPLAY", "  "), ("REPLAY_DIRECTORY", "   "));
        Assert.That((blank.WebReplay, blank.ReplayDirectory, blank.KeepsRecordings),
            Is.EqualTo(((string?)null, (string?)null, false)));
        Assert.That(With().KeepsRecordings, Is.False);
    }

    private static AgentOptions With(params (string Name, string Value)[] variables) =>
        AgentOptions.FromEnvironment(name => variables.FirstOrDefault(v => v.Name == name).Value);

    [Test]
    public void RolesEnabled_ReadsRolesEnabled() =>
        Assert.That(With(("ROLES_ENABLED", SampleRole.Fill("<RoleAlpha>,puls/voice-af"))).RolesEnabled,
            Is.EqualTo(new[] { SampleRole.Alpha, "puls/voice-af" }));

    [Test]
    public void RolesEnabled_ReadsThePersonalitiesSetting_WhenItIsUnset() =>
        Assert.That(With(("PERSONALITIES_ENABLED", SampleRole.Alpha)).RolesEnabled,
            Is.EqualTo(new[] { SampleRole.Alpha }));

    [Test]
    public void RolesEnabled_ReadsTheNewOne_WhenBothAreSet() =>
        Assert.That(With(("ROLES_ENABLED", SampleRole.Alpha), ("PERSONALITIES_ENABLED", "puls/voice-af")).RolesEnabled,
            Is.EqualTo(new[] { SampleRole.Alpha }));

    // Review of PR #60: ROLES_ENABLED= (empty: every role on) is set, not unset, so the old name never overrides it.
    [Test]
    public void AnEmptyRolesEnabled_TurnsEveryRoleOn_EvenWithTheOldNameSet() =>
        Assert.That(With(("ROLES_ENABLED", ""), ("PERSONALITIES_ENABLED", SampleRole.Alpha)).RolesEnabled, Is.Empty);

    private static string Stderr(Action action)
    {
        TextWriter original = Console.Error;
        var stderr = new StringWriter();
        Console.SetError(stderr);
        try
        {
            LouisAgent.Core.Logging.AgentLogging.Configure(null);
            action();
        }
        finally
        {
            Console.SetError(original);
        }

        return stderr.ToString();
    }

    // Review of PR #60: Puls's compose sets ROLES_ENABLED, and env_file passes config/.env's old name in too, so the
    // warning must also show when both are set, saying the old one is ignored.
    [Test, NonParallelizable]
    public void TheOldSetting_BesideTheNewOne_IsSaidToBeIgnored() =>
        Assert.That(Stderr(() => With(("ROLES_ENABLED", SampleRole.Alpha), ("PERSONALITIES_ENABLED", "puls/voice-af"))),
            Does.Contain("[WARN]").And.Contain("PERSONALITIES_ENABLED").And.Contain("ignored"));

    [Test, NonParallelizable]
    public void TheOldSetting_IsNamedInAWarning()
    {
        TextWriter original = Console.Error;
        var stderr = new StringWriter();
        Console.SetError(stderr);
        try
        {
            LouisAgent.Core.Logging.AgentLogging.Configure(null);
            With(("PERSONALITIES_ENABLED", SampleRole.Alpha));
        }
        finally
        {
            Console.SetError(original);
        }

        Assert.That(stderr.ToString(),
            Does.Contain("[WARN]").And.Contain("PERSONALITIES_ENABLED").And.Contain("ROLES_ENABLED"));
    }
}
