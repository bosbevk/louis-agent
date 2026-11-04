using LouisAgent.Core.Hosting.SessionLlms;
using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Llm;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Tests.Hosting.SessionLlms;

// F30 step 1: the models a session may name are LLM profiles, read from config/.env.{name} without touching the process
// environment, so the instance's own model and every other setting stay as they were.
public class SessionLlmLoaderTests
{
    private string _config = "";

    [SetUp]
    public void SetUp()
    {
        _config = Path.Combine(Path.GetTempPath(), "session-llm-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_config);
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_config, recursive: true);

    /// <summary>Records the options each client was built from; the client itself is a stand-in.</summary>
    private sealed class RecordingFactory : ILlmClientFactory
    {
        public List<LlmOptions> Built { get; } = [];

        public IChatClient Create(LlmOptions options)
        {
            Built.Add(options);
            return new FakeChatClient();
        }
    }

    [Test]
    public void AProfile_IsReadFromItsFile_AndTheInstancesModelDoesntLeakIn()
    {
        File.WriteAllText(Path.Combine(_config, ".env.ollama-puls"),
            "# a local model for prices\nLLM_PROVIDER=ollama\nLLM_MODEL=qwen3:4b-instruct-2507-q4_K_M\nLLM_ENDPOINT=http://host.docker.internal:11434\n");
        // The instance runs Claude with thinking on; none of that belongs to the local profile.
        var environment = new Dictionary<string, string>
        {
            ["LLM_PROVIDER"] = "anthropic", ["LLM_MODEL"] = "claude-haiku-5-5", ["LLM_THINKING"] = "high",
        };
        var factory = new RecordingFactory();
        var agent = new AgentOptions { SessionLlmProfiles = ["ollama-puls"] };

        var llms = SessionLlmLoader.Load(agent, factory, _config, name => environment.GetValueOrDefault(name));

        Assert.That(llms.Select(l => l.Name), Is.EqualTo(new[] { "ollama-puls" }));
        var built = factory.Built.Single();
        Assert.That((built.Provider, built.Model, built.Endpoint), Is.EqualTo(("ollama", "qwen3:4b-instruct-2507-q4_K_M", "http://host.docker.internal:11434")));
        Assert.That(llms[0].Thinking, Is.Null, "the instance's LLM_THINKING isn't the profile's");
    }

    [Test]
    public void AnAnthropicProfile_TakesTheApiKey_FromTheEnvironment()
    {
        // Secrets stay in .env.secrets, never in a profile file; only the key names fall back to the environment.
        File.WriteAllText(Path.Combine(_config, ".env.sonnet"), "LLM_PROVIDER=anthropic\nLLM_MODEL=claude-sonnet-5-5\n");
        var factory = new RecordingFactory();

        SessionLlmLoader.Load(new AgentOptions { SessionLlmProfiles = ["sonnet"] }, factory, _config,
            name => name == "ANTHROPIC_API_KEY" ? "sk-test" : null);

        Assert.That(factory.Built.Single().ApiKey, Is.EqualTo("sk-test"));
    }

    [Test]
    public void AListedProfileWithoutItsFile_StopsStartUp_NamingTheFile()
    {
        var refused = Assert.Throws<SessionLlmException>(() =>
            SessionLlmLoader.Load(new AgentOptions { SessionLlmProfiles = ["ollama-puls"] }, new RecordingFactory(), _config, _ => null))!;

        Assert.That(refused.Message, Does.Contain("ollama-puls").And.Contain(".env.ollama-puls"));
    }

    [Test]
    public void NoProfilesListed_IsNoSessionModels()
    {
        Assert.That(SessionLlmLoader.Load(new AgentOptions(), new RecordingFactory(), _config, _ => null), Is.Empty);
    }
}
