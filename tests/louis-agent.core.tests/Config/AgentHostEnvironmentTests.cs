namespace louis_agent.core.tests;

// LLM profiles: config/.env.{LLM_PROFILE} fills in what the shell, .env.secrets and .env left unset.
// The fake config files use their own variable names, so the real LLM_* settings of other tests are never touched.
[NonParallelizable]
public class AgentHostEnvironmentTests
{
    private const string Variable = "LOUIS_AGENT_TEST_MODEL";
    private string _repo = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = Directory.CreateTempSubdirectory("env-profile-").FullName;
        Directory.CreateDirectory(Path.Combine(_repo, "config"));
        Clear();
    }

    [TearDown]
    public void TearDown()
    {
        Clear();
        Directory.Delete(_repo, recursive: true);
    }

    private static void Clear()
    {
        Environment.SetEnvironmentVariable("LLM_PROFILE", null);
        Environment.SetEnvironmentVariable(Variable, null);
    }

    private void Write(string fileName, string content) => File.WriteAllText(Path.Combine(_repo, "config", fileName), content);

    [Test]
    public void LoadEnvironment_ProfileInDotEnv_LoadsTheProfileFile()
    {
        Write(".env", "LLM_PROFILE=ollama\n");
        Write(".env.ollama", $"{Variable}=qwen2.5-coder\n");
        Write(".env.anthropic", $"{Variable}=claude\n");

        AgentHost.LoadEnvironment(_repo);

        Assert.That(Environment.GetEnvironmentVariable(Variable), Is.EqualTo("qwen2.5-coder"));
    }

    [Test]
    public void LoadEnvironment_SettingInDotEnv_OverridesTheProfile()
    {
        Write(".env", $"LLM_PROFILE=ollama\n{Variable}=pinned-in-dotenv\n");
        Write(".env.ollama", $"{Variable}=qwen2.5-coder\n");

        AgentHost.LoadEnvironment(_repo);

        Assert.That(Environment.GetEnvironmentVariable(Variable), Is.EqualTo("pinned-in-dotenv"));
    }

    [Test]
    public void LoadEnvironment_ProfileFromTheShell_WinsOverDotEnv()
    {
        // `LLM_PROFILE=ollama dotnet run ...` switches one run without editing .env.
        Environment.SetEnvironmentVariable("LLM_PROFILE", "ollama");
        Write(".env", "LLM_PROFILE=anthropic\n");
        Write(".env.ollama", $"{Variable}=qwen2.5-coder\n");
        Write(".env.anthropic", $"{Variable}=claude\n");

        AgentHost.LoadEnvironment(_repo);

        Assert.That(Environment.GetEnvironmentVariable(Variable), Is.EqualTo("qwen2.5-coder"));
    }

    [Test]
    public void LoadEnvironment_NoProfile_LoadsNoProfileFile()
    {
        Write(".env", "# no LLM_PROFILE\n");
        Write(".env.anthropic", $"{Variable}=claude\n");

        AgentHost.LoadEnvironment(_repo);

        Assert.That(Environment.GetEnvironmentVariable(Variable), Is.Null);
    }

    [TestCase("missing")]
    [TestCase("../secrets")]
    public void LoadEnvironment_UnknownOrInvalidProfile_LoadsNothingAndWarns(string profile)
    {
        Write(".env", $"LLM_PROFILE={profile}\n");
        Write(".env.secrets", "");
        var stderr = new StringWriter();
        var original = Console.Error;
        Console.SetError(stderr);
        try
        {
            AgentHost.LoadEnvironment(_repo);
        }
        finally
        {
            Console.SetError(original);
        }

        Assert.That(stderr.ToString(), Does.Contain($"LLM_PROFILE '{profile}'"));
        Assert.That(Environment.GetEnvironmentVariable(Variable), Is.Null);
    }
}
