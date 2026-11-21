using LouisAgent.Choices;

namespace LouisAgent.Choices.Tests;

// F32 step 1b (Louis: "can we make this enums … This must also be a rule"): a setting's allowed values are an enum,
// parsed once where the setting is read. SettingChoice is the one parser: a name with its case ignored, or an alias;
// never a number (Enum.TryParse takes "1"); and the names the "supported" messages list, from the enum itself.
public class SettingChoiceTests
{
    public enum Engine
    {
        Tavily,
        Google,
        DuckDuckGo
    }

    private static readonly IReadOnlyDictionary<string, Engine> Aliases = new Dictionary<string, Engine>
    { ["ddg"] = Engine.DuckDuckGo };

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Unset_IsNoChoice(string? value)
    {
        Assert.That(SettingChoice.TryParse<Engine>(value, out var choice), Is.True);
        Assert.That(choice, Is.Null);
    }

    [TestCase("tavily", Engine.Tavily)]
    [TestCase("GOOGLE", Engine.Google)]
    [TestCase(" DuckDuckGo ", Engine.DuckDuckGo)]
    [TestCase("ddg", Engine.DuckDuckGo)]
    [TestCase("DDG", Engine.DuckDuckGo)]
    public void AName_OrAnAlias_IgnoringCase(string value, Engine expected)
    {
        Assert.That(SettingChoice.TryParse(value, out Engine? choice, Aliases), Is.True);
        Assert.That(choice, Is.EqualTo(expected));
    }

    [TestCase("bing")]
    [TestCase("1")]
    [TestCase("0")]
    [TestCase("Tavily,Google")]
    public void AnythingElse_IsUnknown(string value)
    {
        Assert.That(SettingChoice.TryParse(value, out Engine? choice, Aliases), Is.False);
        Assert.That(choice, Is.Null);
    }

    [Test]
    public void TheNames_AreTheEnumsInItsOrder_InLowerCase() =>
        Assert.That(SettingChoice.Names<Engine>(), Is.EqualTo(new[] { "tavily", "google", "duckduckgo" }));
}
