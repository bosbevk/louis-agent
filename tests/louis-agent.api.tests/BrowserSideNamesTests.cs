using LouisAgent.Choices;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Web.Shared.Api;
using CoreScriptLanguage = LouisAgent.Core.Skills.ScriptLanguage;
using WebScriptLanguage = LouisAgent.Web.Shared.Api.ScriptLanguage;

namespace LouisAgent.Api.Tests;

// F18 step 7.5.3 (ADR 0043, Louis's card 3): the browser can't reference core, so the web apps keep their own small enums of the names they use (`AskedToolset`, `ScriptLanguage` in web.shared).
// This test sits where both sides are visible and checks them against core's, so the two copies cannot drift: a name the browser asks for that core does not have, or a language core gained that the browser lacks.
public class BrowserSideNamesTests
{
    [Test]
    public void EveryToolsetTheAppsAsk_IsACoreToolset_UnderTheSameName()
    {
        foreach (var asked in Enum.GetValues<AskedToolset>())
        {
            Assert.That(SettingChoice.TryParse(asked.Name(), out CoreToolset? core), Is.True, $"{asked}: core has no toolset of that name");
            Assert.That(core!.Value.Name(), Is.EqualTo(asked.Name()), asked.ToString());
        }
    }

    [Test]
    public void TheBrowsersScriptLanguages_AreCoresScriptLanguages_NameForName() =>
        Assert.That(Enum.GetValues<WebScriptLanguage>().Select(SettingChoice.Wire).Order(),
            Is.EqualTo(Enum.GetValues<CoreScriptLanguage>().Select(SettingChoice.Wire).Order()));
}
