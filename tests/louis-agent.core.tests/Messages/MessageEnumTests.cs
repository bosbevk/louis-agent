using System.Reflection;
using LouisAgent.Core.Config.Messages;
using LouisAgent.Core.Messages;

namespace LouisAgent.Core.Tests.Messages;

/// <summary>
/// Every message is an enum member whose [Description] is its template, with named holes (docs/CLAUDE.md, *Messages
/// and logging*). This checks every message enum in core, so a member added without text, or with a broken hole,
/// fails here. The message enums are the enums of a <c>Messages</c> folder at any depth (the split by area, F18): an
/// area's enum sits in its own area's <c>Messages</c> namespace, and the shared ones in core's.
/// </summary>
public class MessageEnumTests
{
    /// <summary>Every enum of a <c>Messages</c> namespace in core.</summary>
    private static readonly Type[] MessageEnums =
        typeof(MessageText).Assembly.GetTypes().Where(t => t.IsEnum && t.Namespace?.Split('.').Last() == "Messages")
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
                "ConfigErrorMessage", "ConfigLogMessage", "CurrencyToolMessage", "DevOpsToolMessage",
                "DocumentToolMessage", "DotNetToolMessage", "ErrorMessage", "GitToolMessage",
                "GuardrailErrorMessage", "GuardrailToolMessage", "HostingErrorMessage", "HostingLogMessage",
                "LlmErrorMessage", "LoggingErrorMessage", "LoggingLogMessage", "PaymoToolMessage",
                "PricingErrorMessage", "PricingLogMessage", "PromptHeaderMessage", "ProtocolLogMessage",
                "ScriptToolMessage", "SessionErrorMessage", "ShellToolMessage", "SkillErrorMessage",
                "SkillLogMessage", "SubSessionToolMessage", "TelemetryLogMessage", "ToolMessage", "UserText",
                "UsageErrorMessage", "UsageLogMessage", "WebLogMessage", "WebToolMessage", "WorkspaceToolMessage",
            }));
    }

    /// <summary>An enum named for messages or texts lives in a <c>Messages</c> folder.</summary>
    [Test]
    public void EveryMessageOrTextEnum_LivesInAMessagesFolder()
    {
        // An enum named for messages but placed outside a Messages folder would skip every check above, so it fails
        // here.
        var stray = typeof(MessageText).Assembly.GetTypes()
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
            string withoutHoles =
                System.Text.RegularExpressions.Regex.Replace(template, @"\{[A-Za-z][A-Za-z0-9]*\}", "");
            Assert.That(withoutHoles, Does.Not.Contain("{").And.Not.Contain("}"),
                $"{member.GetType().Name}.{member}: \"{template}\"");
        }
    }

    /// <summary>A message's holes are filled by name.</summary>
    [Test]
    public void Format_FillsNamedHoles()
    {
        Assert.That(MessageText.Format(ConfigErrorMessage.UnknownCacheTtl, new { Value = "10m" }),
            Is.EqualTo("Unknown LLM_CACHE_TTL '10m'. Use 5m or 1h."));
    }

    /// <summary>A hole with no value throws, naming the member and the hole.</summary>
    [Test]
    public void Format_AMissingValue_Throws()
    {
        // A message never goes out with a gap in it.
        Assert.That(() => MessageText.Format(ConfigErrorMessage.UnknownCacheTtl, new { Other = "x" }),
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
