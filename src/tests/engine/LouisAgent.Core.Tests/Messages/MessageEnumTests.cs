using LouisAgent.Core.Web;
using LouisAgent.Core.Anthropic;
using LouisAgent.Core.Ollama;
using LouisAgent.Core.OpenAi;
using LouisAgent.Core.Config;
using LouisAgent.Composition.Messages;
using System.Reflection;
using LouisAgent.Core.Config.Messages;
using LouisAgent.Testing.Messages;

namespace LouisAgent.Core.Tests.Messages;

/// <summary>
/// Every message is an enum member whose [Description] is its template, with named holes (src/docs/CLAUDE.md, *Messages
/// and logging*). This checks every message enum in core, so a member added without text, or with a broken hole,
/// fails here. The message enums are the enums of a <c>Messages</c> folder at any depth (the split by area, F18): an
/// area's enum sits in its own area's <c>Messages</c> namespace, and the shared ones in core's.
/// </summary>
public class MessageEnumTests
{
    /// <summary>Every enum of a <c>Messages</c> namespace in core.</summary>
    private static readonly Type[] MessageEnums =
        typeof(AgentOptions).Assembly.GetTypes().Concat(typeof(MessageText).Assembly.GetTypes())
            .Concat(typeof(WebAccess).Assembly.GetTypes()).Concat(typeof(AnthropicProvider).Assembly.GetTypes())
            .Concat(typeof(OllamaProvider).Assembly.GetTypes())
            .Concat(typeof(OpenAiCompatibleProvider).Assembly.GetTypes())
            .Where(t => t.IsEnum && t.Namespace?.Split('.').Last() == "Messages")
            .ToArray();

    /// <summary>Every member of every message enum.</summary>
    private static IEnumerable<Enum> AllMembers() =>
        MessageEnums.SelectMany(t => Enum.GetValues(t).Cast<Enum>());

    /// <summary>The message enums are the ones we know of: a new one is added here on purpose.</summary>
    [Test]
    public void TheMessageEnumsExist()
    {
        Assert.That(MessageEnums.Select(t => t.Name),
            Is.EquivalentTo(new[]
            {
                "AgentErrorMessage", "AgentLogMessage", "AgentToolMessage", "ClockToolMessage",
                "ConfigErrorMessage", "ConfigLogMessage", "ErrorMessage", "ScreenMessage", "GuardrailErrorMessage",
                "GuardrailToolMessage", "HostingErrorMessage", "HostingLogMessage", "LlmErrorMessage",
                "LoggingErrorMessage", "LoggingLogMessage", "PricingErrorMessage", "PricingLogMessage",
                "PromptHeaderMessage", "ScriptToolMessage", "AnthropicErrorMessage", "OpenAiErrorMessage",
                "SessionErrorMessage", "RunnerToolMessage", "SkillErrorMessage", "SkillLogMessage",
                "SkillReadToolMessage", "SubSessionToolMessage", "TelemetryLogMessage", "TextErrorMessage",
                "ToolMessage", "UserText", "UsageErrorMessage", "UsageLogMessage", "WebLogMessage",
                "WebToolMessage",
            }));
    }

    /// <summary>An enum named for messages or texts lives in a <c>Messages</c> folder.</summary>
    [Test]
    public void EveryMessageOrTextEnum_LivesInAMessagesFolder()
    {
        // An enum named for messages but placed outside a Messages folder would skip every check above, so it fails
        // here.
        var stray = typeof(AgentOptions).Assembly.GetTypes().Concat(typeof(MessageText).Assembly.GetTypes())
            .Concat(typeof(WebAccess).Assembly.GetTypes()).Concat(typeof(AnthropicProvider).Assembly.GetTypes())
            .Concat(typeof(OllamaProvider).Assembly.GetTypes())
            .Concat(typeof(OpenAiCompatibleProvider).Assembly.GetTypes())
            .Where(t => t.IsEnum && (t.Name.EndsWith("Message", StringComparison.Ordinal) ||
                                     t.Name.EndsWith("Text", StringComparison.Ordinal)))
            .Where(t => t.Namespace?.Split('.').Last() != "Messages")
            .Select(t => t.FullName)
            .ToList();

        Assert.That(stray, Is.Empty);
    }

    /// <summary>Every member has its text as a [Description].</summary>
    [Test]
    public void EveryMember_HasATemplate()
    {
        foreach (Enum member in AllMembers())
        {
            string? text = member.GetType().GetField(member.ToString())!
                .GetCustomAttribute<System.ComponentModel.DescriptionAttribute>()?.Description;
            Assert.That(text, Is.Not.Null.And.Not.Empty, $"{member.GetType().Name}.{member} has no [Description]");
        }
    }

    /// <summary>Every brace in a template belongs to a named hole.</summary>
    [Test]
    public void EveryTemplate_HasOnlyWellFormedNamedHoles()
    {
        // Every '{' or '}' must belong to a named hole ({Value}); no positional {0}, no stray or unclosed braces.
        foreach (Enum member in AllMembers())
        {
            string template = MessageText.Template(member);
            string withoutHoles = MessageText.Holes(template)
                .Aggregate(template, (text, hole) => text.Replace($"{{{hole}}}", "", StringComparison.Ordinal));
            Assert.That(withoutHoles, Does.Not.Contain("{").And.Not.Contain("}"),
                $"{member.GetType().Name}.{member}: \"{template}\"");
        }
    }

    /// <summary>The value for the cache-time message's one hole.</summary>
    private sealed record CacheTtl(string Value);

    /// <summary>A value under another name than the hole's.</summary>
    private sealed record OtherValue(string Other);

    /// <summary>
    /// ADR 0119: every member that names its holes record has the record's properties equal to its template's holes.
    /// </summary>
    [Test]
    public void EveryHolesRecord_MatchesItsTemplate() =>
        Assert.That(MessageHoles.Problems(MessageEnums), Is.Empty);

    /// <summary>A message's holes are filled by name.</summary>
    [Test]
    public void Format_FillsNamedHoles()
    {
        var tenMinutes = new CacheTtl("10m");

        Assert.That(MessageText.Format(ConfigErrorMessage.UnknownCacheTtl, tenMinutes),
            Is.EqualTo("Unknown LLM_CACHE_TTL '10m'. Use 5m or 1h."));
    }

    /// <summary>A hole with no value throws, naming the member and the hole.</summary>
    [Test]
    public void Format_AMissingValue_Throws()
    {
        var misnamed = new OtherValue("x");

        // A message never goes out with a gap in it.
        Assert.That(() => MessageText.Format(ConfigErrorMessage.UnknownCacheTtl, misnamed),
            Throws.ArgumentException.With.Message.Contain(
                "ConfigErrorMessage.UnknownCacheTtl needs a value for '{Value}'"));
    }

    /// <summary>The template keeps its holes, so a structured logger makes them fields.</summary>
    [Test]
    public void Template_KeepsTheHolesForAStructuredLogger()
    {
        Assert.That(MessageText.Template(ConfigErrorMessage.UnknownCacheTtl),
            Is.EqualTo("Unknown LLM_CACHE_TTL '{Value}'. Use 5m or 1h."));
        Assert.That(MessageText.Holes(MessageText.Template(ConfigErrorMessage.UnknownCacheTtl)),
            Is.EqualTo(new[] { "Value" }));
    }
}
