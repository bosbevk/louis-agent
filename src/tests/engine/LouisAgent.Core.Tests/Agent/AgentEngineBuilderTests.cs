using LouisAgent.Core.Agent;
using LouisAgent.Core.Config;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Prompting.Providers;
using LouisAgent.Testing;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Tests.Agent;

/// <summary>
/// F97.1 step 4b: an engine is built from the three things every agent needs, its skills, its model and its settings,
/// and a call for each other part; a part not called keeps the constructor's default, so a builder with no calls makes
/// the same engine the constructor does.
/// </summary>
public class AgentEngineBuilderTests
{
    private static readonly AgentOptions Options = new() { WorkspaceRoot = Environment.CurrentDirectory };

    /// <summary>
    /// With no calls, the engine is the constructor's: its tools, its name, its thinking and the rest.
    /// </summary>
    [Test]
    public void ABuilderWithNoCalls_MakesTheConstructorsEngine()
    {
        var skills = new CompositeSkillProvider();

        var built = AgentEngine.CreateBuilder(skills, new FakeChatClient(), Options).Build();
        var constructed = new AgentEngine(skills, new FakeChatClient(), Options);

        Assert.Multiple(() =>
        {
            Assert.That((built.SupportsTools, built.RecordsUsage, built.Name, built.Thinking),
                Is.EqualTo(
                    (constructed.SupportsTools, constructed.RecordsUsage, constructed.Name, constructed.Thinking)));
            Assert.That(built.Tools.Select(t => t.Name), Is.EqualTo(constructed.Tools.Select(t => t.Name)));
        });
    }

    /// <summary>An engine given no name takes the host's service name, as the constructor's does.</summary>
    [Test]
    public void AnEngineWithoutAName_TakesTheServiceName()
    {
        var engine = AgentEngine.CreateBuilder(new CompositeSkillProvider(), new FakeChatClient(), Options).Build();

        Assert.That(engine.Name, Is.EqualTo(AgentLogging.ServiceName));
    }

    /// <summary>Each part called is the engine's.</summary>
    [Test]
    public void EachPartCalled_IsTheEngines()
    {
        var engine = AgentEngine.CreateBuilder(new CompositeSkillProvider(), new FakeChatClient(), Options)
            .WithSupportsTools(false)
            .WithName("reviewer")
            .WithThinking(ReasoningEffort.Medium)
            .WithSkillsDirectory("skills-here")
            .Build();

        Assert.That((engine.SupportsTools, engine.Name, engine.Thinking, engine.SkillsDirectory),
            Is.EqualTo((false, "reviewer", (ReasoningEffort?)ReasoningEffort.Medium, "skills-here")));
    }

    /// <summary>A builder keeps nothing of what it built: building twice makes two engines of the same parts.</summary>
    [Test]
    public void BuildingTwice_MakesTwoEngines()
    {
        var builder = AgentEngine.CreateBuilder(new CompositeSkillProvider(), new FakeChatClient(), Options)
            .WithName("same-parts");

        var first = builder.Build();
        var second = builder.Build();

        Assert.That((ReferenceEquals(first, second), second.Name), Is.EqualTo((false, first.Name)));
    }

    /// <summary>An engine can't be built without its skills or its model.</summary>
    [Test]
    public void ABuilderWithoutSkillsOrAModel_IsRefused()
    {
        var model = new FakeChatClient();
        var skills = new CompositeSkillProvider();

        Assert.Multiple(() =>
        {
            Assert.That(() => AgentEngine.CreateBuilder(null!, model, Options), Throws.ArgumentNullException);
            Assert.That(() => AgentEngine.CreateBuilder(skills, null!, Options), Throws.ArgumentNullException);
        });
    }
}
