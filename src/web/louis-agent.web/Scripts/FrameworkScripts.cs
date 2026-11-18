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

    /// <summary>
    /// The chat shell's page-wide behaviour: the sidebar, the work panel, the cards, the costs, focus, copy.
    /// </summary>
    public const string Shell = Content + "Components/Chat/Shell/ChatShell.razor.js";

    /// <summary>The message list's scrolling.</summary>
    public const string MessageList = Content + "Components/Chat/MessageList.razor.js";

    /// <summary>
    /// A site frame's motion: the progress line, the compact header, the in-page links, the reveals (F110).
    /// </summary>
    public const string SiteShell = Content + "Components/Site/SiteShell.razor.js";

    /// <summary>The section index: the entries in the page's order, the one in view lit (F118).</summary>
    public const string SectionIndex = Content + "Components/Site/Index/SectionIndex.razor.js";

    /// <summary>A code listing's copy: its code written to the clipboard as it stands (F161.1).</summary>
    public const string CodeListing = Content + "Components/Site/Code/CodeListing.razor.js";

    /// <summary>The look switch: the theme and style written on the page, remembered, and followed (F151.3).</summary>
    public const string LookSwitch = Content + "Components/Site/Looks/LookSwitch.razor.js";

    /// <summary>
    /// The message box's keys: Enter sends, Shift+Enter breaks the line, wherever the box is drawn (FX70).
    /// </summary>
    public const string Composer = Content + "Components/Chat/Composer.razor.js";

    /// <summary>A showroom's mock website going to the place its agent showed, inside the mock (F127.1).</summary>
    public const string Mock = Content + "Components/Site/Showroom/Mock/SiteMock.razor.js";

    /// <summary>
    /// A deck as a row on a phone: which item a swipe left in view, and bringing one into view (F134.1).
    /// </summary>
    public const string Deck = Content + "Components/Staging/Decks/Deck.razor.js";

    /// <summary>The address bar following the demo that is forward on a showroom's start (F134.1).</summary>
    public const string ShowroomStart = Content + "Components/Site/Showroom/Start/ShowroomStart.razor.js";

    /// <summary>The page the visitor is reading, for the guide in a site's corner (F115).</summary>
    public const string Widget = Content + "Components/Widget/ChatWidget.razor.js";

    /// <summary>The room the agent's bar takes at the page's foot, told to the page (F134.3).</summary>
    public const string Bar = Content + "Components/Widget/Bar/AgentBar.razor.js";

    /// <summary>The agent's caption standing beside the place it lit, on a wide screen (F134.4).</summary>
    public const string Caption = Content + "Components/Widget/Bar/Caption/AgentCaption.razor.js";

    /// <summary>
    /// The agent's caption growing in from where it comes from, and fading before it goes (F154). Its caption already
    /// has a module of its own beside it, and the library serves a second only from <c>wwwroot/js</c>.
    /// </summary>
    public const string CaptionMotion = Content + "js/caption-motion.js";

    /// <summary>A piece the visitor selects for the guide, ringed on the page (F127.7).</summary>
    public const string PieceWatch = Content + "Components/Widget/Pieces/PieceWatch.razor.js";

    /// <summary>A turn's code blocks, highlighted on the site's highlight.js.</summary>
    public const string TurnView = Content + "Components/TurnView.razor.js";

    /// <summary>What the browser remembers for the person: the place, the language, motion.</summary>
    public const string Preferences = Content + "js/preferences.js";

    /// <summary>Focus for a dialog or a field Blazor has just drawn.</summary>
    public const string Dialogs = Content + "js/dialogs.js";

    /// <summary>A file the page fetched, handed to the browser as a download.</summary>
    public const string Download = Content + "js/download.js";

    /// <summary>
    /// This browser's saved lists of chats, and forgetting all it keeps but one key, for Reset my session (F153).
    /// </summary>
    public const string Reset = Content + "Components/Admin/Reset/ResetSession.razor.js";
}
