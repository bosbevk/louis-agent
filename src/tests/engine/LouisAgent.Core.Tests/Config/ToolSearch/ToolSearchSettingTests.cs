using LouisAgent.Core.Config;
using LouisAgent.Core.Config.ToolSearch;

namespace LouisAgent.Core.Tests.Config.ToolSearch;

/// <summary>
/// TOOL_SEARCH (F97.3): whether a chat finds its tools by a search, parsed once where it enters. Unset is auto; a word
/// it doesn't know stops start-up, naming the ones it does.
/// </summary>
public class ToolSearchSettingTests
{
    /// <summary>Unset is auto, and each mode reads in any case.</summary>
    [TestCase(null, ToolSearchMode.Auto)]
    [TestCase("", ToolSearchMode.Auto)]
    [TestCase("auto", ToolSearchMode.Auto)]
    [TestCase("on", ToolSearchMode.On)]
    [TestCase(" OFF ", ToolSearchMode.Off)]
    public void ASetting_IsReadAsItsMode(string? setting, ToolSearchMode mode) =>
        Assert.That(ToolSearchSetting.Parse(setting), Is.EqualTo(mode));

    /// <summary>A word it doesn't know stops start-up, and the message names the setting and its modes.</summary>
    [Test]
    public void AnUnknownWord_IsRefused_NamingTheModes()
    {
        var refused = Assert.Throws<ArgumentException>(() => ToolSearchSetting.Parse("sometimes"));

        Assert.That(refused!.Message,
            Does.Contain("TOOL_SEARCH").And.Contain("sometimes").And.Contain("auto, on or off"));
    }

    /// <summary>The host's settings carry it.</summary>
    [Test]
    public void TheSettings_ReadIt()
    {
        var options = AgentOptions.FromEnvironment(name => name == "TOOL_SEARCH" ? "off" : null);

        Assert.That(options.ToolSearch, Is.EqualTo(ToolSearchMode.Off));
    }
}
