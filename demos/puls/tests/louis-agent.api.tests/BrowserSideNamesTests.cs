using LouisAgent.Packs.Web;
using LouisAgent.Choices;
using LouisAgent.Composition.Packs.Screens;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Web.Api;
using LouisAgent.Web.Components.Screens.Table;
using CoreScriptLanguage = LouisAgent.Core.Skills.Languages.ScriptLanguage;
using WebScriptLanguage = LouisAgent.Web.Api.ScriptLanguage;

namespace LouisAgent.Api.Tests;

/// <summary>
/// The browser can't reference core, so the web apps keep their own small enums of the names they use (ADR 0043). This
/// test sits where both sides are visible and checks them against core's, so the two copies can't drift: a name the
/// browser asks for that core doesn't have, or a language core gained that the browser lacks.
/// </summary>
public class BrowserSideNamesTests
{
    /// <summary>Every toolset the browser asks for is one core has, under the same name.</summary>
    [Test]
    public void EveryToolsetTheAppsAsk_IsAToolset_UnderTheSameName()
    {
        var known = SettingChoice.Names<CoreToolset>().Concat(SettingChoice.Names<WebToolset>()).ToList();

        foreach (var asked in Enum.GetValues<AskedToolset>())
            Assert.That(known, Does.Contain(asked.Wire()), $"{asked}: no toolset has that name");
    }

    /// <summary>The browser's script languages are core's, name for name.</summary>
    [Test]
    public void TheBrowsersScriptLanguages_AreCoresScriptLanguages_NameForName() =>
        Assert.That(Enum.GetValues<WebScriptLanguage>().Select(m => m.Wire()).Order(),
            Is.EqualTo(Enum.GetValues<CoreScriptLanguage>().Select(m => m.Wire()).Order()));

    /// <summary>The kinds of column the browser can draw are the ones a pack can say, name for name.</summary>
    [Test]
    public void TheBrowsersCellKinds_AreThePacksCellKinds_NameForName() =>
        Assert.That(Enum.GetValues<ScreenCellKind>().Select(kind => kind.Wire()).Order(),
            Is.EqualTo(Enum.GetValues<ScreenCell>().Select(kind => kind.Wire()).Order()));
}
