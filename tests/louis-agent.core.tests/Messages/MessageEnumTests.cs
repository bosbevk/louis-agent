using System.Reflection;
using LouisAgent.Core.Messages;

namespace LouisAgent.Core.Tests.Messages;

// docs/CLAUDE.md, "Messages and logging": every message is an enum member whose [Description] is its template, with
// named holes. This checks every message enum in core, so a member added without text, or with a broken hole, fails here.
// The message enums are the enums of a `Messages` folder at any depth (F-refactor, the split by area): an area's enum sits in its
// own area's `Messages` namespace, and the shared ones in core's.
public class MessageEnumTests
{
    private static readonly Type[] MessageEnums =
        typeof(MessageText).Assembly.GetTypes().Where(t => t.IsEnum && t.Namespace?.Split('.').Last() == "Messages").ToArray();

    private static IEnumerable<Enum> AllMembers() =>
        MessageEnums.SelectMany(t => Enum.GetValues(t).Cast<Enum>());

    [Test]
    public void TheMessageEnumsExist()
    {
        Assert.That(MessageEnums.Select(t => t.Name), Is.EquivalentTo(new[]
        {
            "AgentToolMessage", "ClockToolMessage", "CurrencyToolMessage", "DevOpsToolMessage", "DocumentToolMessage", "DotNetToolMessage",
            "ErrorMessage", "GitToolMessage", "GuardrailToolMessage", "LogMessage", "PaymoToolMessage", "ReplayMessage", "ScriptToolMessage",
            "ShellToolMessage", "SubSessionToolMessage", "ToolMessage", "UserText", "WebToolMessage", "WorkspaceToolMessage",
        }));
    }

    [Test]
    public void EveryMessageOrTextEnum_LivesInAMessagesFolder()
    {
        // An enum named for messages but placed outside a Messages folder would skip every check above, so it fails here.
        var stray = typeof(MessageText).Assembly.GetTypes()
            .Where(t => t.IsEnum && (t.Name.EndsWith("Message", StringComparison.Ordinal) || t.Name.EndsWith("Text", StringComparison.Ordinal)))
            .Where(t => t.Namespace?.Split('.').Last() != "Messages")
            .Select(t => t.FullName)
            .ToList();

        Assert.That(stray, Is.Empty);
    }

    [Test]
    public void EveryMember_HasATemplate()
    {
        foreach (Enum member in AllMembers())
        {
            string? text = member.GetType().GetField(member.ToString())!.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>()?.Description;
            Assert.That(text, Is.Not.Null.And.Not.Empty, $"{member.GetType().Name}.{member} has no [Description]");
        }
    }

    [Test]
    public void EveryTemplate_HasOnlyWellFormedNamedHoles()
    {
        // Every '{' or '}' must belong to a named hole ({Value}); no positional {0}, no stray or unclosed braces.
        foreach (Enum member in AllMembers())
        {
            string template = MessageText.Template(member);
            string withoutHoles = System.Text.RegularExpressions.Regex.Replace(template, @"\{[A-Za-z][A-Za-z0-9]*\}", "");
            Assert.That(withoutHoles, Does.Not.Contain("{").And.Not.Contain("}"), $"{member.GetType().Name}.{member}: \"{template}\"");
        }
    }

    [Test]
    public void Format_FillsNamedHoles()
    {
        Assert.That(MessageText.Format(ErrorMessage.UnknownCacheTtl, new { Value = "10m" }), Is.EqualTo("Unknown LLM_CACHE_TTL '10m'. Use 5m or 1h."));
    }

    [Test]
    public void Format_AMissingValue_Throws()
    {
        // A message never goes out with a gap in it.
        Assert.That(() => MessageText.Format(ErrorMessage.UnknownCacheTtl, new { Other = "x" }),
            Throws.ArgumentException.With.Message.Contain("ErrorMessage.UnknownCacheTtl needs a value for '{Value}'"));
    }

    [Test]
    public void Template_KeepsTheHolesForAStructuredLogger()
    {
        Assert.That(MessageText.Template(ErrorMessage.UnknownCacheTtl), Is.EqualTo("Unknown LLM_CACHE_TTL '{Value}'. Use 5m or 1h."));
        Assert.That(MessageText.Holes(MessageText.Template(ErrorMessage.UnknownCacheTtl)), Is.EqualTo(new[] { "Value" }));
    }
}