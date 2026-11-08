namespace LouisAgent.Web.Scripts;

/// <summary>
/// Where the framework's modules are, named once (CL31): a module beside its component is served under the library's
/// content path, and one several components share sits under <c>wwwroot/js</c>. A component builds its
/// <see cref="JsModule"/> from one of these, so a moved file is one change here.
/// </summary>
public static class FrameworkScripts
{
    /// <summary>The library's content path, as a site's page sees it.</summary>
    private const string Content = "./_content/louis-agent.web/";

    /// <summary>The chat shell's page-wide behaviour: the sidebar, the work panel, the cards, the costs, focus, copy.</summary>
    public const string Shell = Content + "Components/Chat/Shell/ChatShell.razor.js";

    /// <summary>The message list's scrolling.</summary>
    public const string MessageList = Content + "Components/Chat/MessageList.razor.js";

    /// <summary>A site frame's motion: the progress line, the compact header, the section index, the reveals (F110).</summary>
    public const string SiteShell = Content + "Components/Site/SiteShell.razor.js";

    /// <summary>The message box's keys: Enter sends, Shift+Enter breaks the line, wherever the box is drawn (FX70).</summary>
    public const string Composer = Content + "Components/Chat/Composer.razor.js";

    /// <summary>A turn's code blocks, highlighted on the site's highlight.js.</summary>
    public const string TurnView = Content + "Components/TurnView.razor.js";

    /// <summary>What the browser remembers for the person: the place, the language, motion.</summary>
    public const string Preferences = Content + "js/preferences.js";

    /// <summary>Focus for a dialog or a field Blazor has just drawn.</summary>
    public const string Dialogs = Content + "js/dialogs.js";

    /// <summary>A file the page fetched, handed to the browser as a download.</summary>
    public const string Download = Content + "js/download.js";
}
