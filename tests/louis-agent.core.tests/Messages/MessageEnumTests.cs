using System.Reflection;
using louis_agent.core.messages;

namespace louis_agent.core.tests;

// docs/CLAUDE.md, "Messages and logging": every message is an enum member whose [Description] is its template, with
// named holes. This checks every message enum in core, so a member added without text, or with a broken hole, fails here.
public class MessageEnumTests
{
    private static readonly Type[] MessageEnums =
        typeof(Messages).Assembly.GetTypes().Where(t => t.IsEnum && t.Namespace == typeof(Messages).Namespace).ToArray();

    private static IEnumerable<Enum> AllMembers() =>
        MessageEnums.SelectMany(t => Enum.GetValues(t).Cast<Enum>());

    [Test]
    public void TheFourMessageEnumsExist()
    {
        Assert.That(MessageEnums.Select(t => t.Name), Is.EquivalentTo(new[] { "ErrorMessage", "LogMessage", "ToolMessage", "UserText" }));
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
            string template = Messages.Template(member);
            string withoutHoles = System.Text.RegularExpressions.Regex.Replace(template, @"\{[A-Za-z][A-Za-z0-9]*\}", "");
            Assert.That(withoutHoles, Does.Not.Contain("{").And.Not.Contain("}"), $"{member.GetType().Name}.{member}: \"{template}\"");
        }
    }

    [Test]
    public void Format_FillsNamedHoles()
    {
        Assert.That(Messages.Format(ErrorMessage.UnknownCacheTtl, new { Value = "10m" }), Is.EqualTo("Unknown LLM_CACHE_TTL '10m'. Use 5m or 1h."));
    }

    [Test]
    public void Format_AMissingValue_Throws()
    {
        // A message never goes out with a gap in it.
        Assert.That(() => Messages.Format(ErrorMessage.UnknownCacheTtl, new { Other = "x" }),
            Throws.ArgumentException.With.Message.Contain("ErrorMessage.UnknownCacheTtl needs a value for '{Value}'"));
    }

    [Test]
    public void Template_KeepsTheHolesForAStructuredLogger()
    {
        Assert.That(Messages.Template(ErrorMessage.UnknownCacheTtl), Is.EqualTo("Unknown LLM_CACHE_TTL '{Value}'. Use 5m or 1h."));
        Assert.That(Messages.Holes(Messages.Template(ErrorMessage.UnknownCacheTtl)), Is.EqualTo(new[] { "Value" }));
    }
}