using LouisAgent.Site.Resumes.Basics;
using LouisAgent.Site.Resumes.Skills;
using LouisAgent.Web.Components.Editing;
using LouisAgent.Web.Components.Editing.Inline;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Site.Components.Cv;

/// <summary>
/// The CV's hero (F142): Louis's name and summary, where he lives, the languages he speaks and where he'd work, a
/// button for each profile (LinkedIn and WhatsApp in their own marks) and his email, and the photo beside them.
/// </summary>
public partial class ResumeHero
{
    /// <summary>The networks drawn with their own brand's mark and button (ADR 0102).</summary>
    private const string LinkedIn = "LinkedIn", WhatsApp = "WhatsApp";

    /// <summary>A link to fill in.</summary>
    private static readonly ResumeProfile Blank = new("", "");

    private int? _open;

    /// <summary>Who he is and how to reach him.</summary>
    [Parameter, EditorRequired]
    public ResumeBasics Basics { get; set; } = default!;

    /// <summary>The languages he speaks.</summary>
    [Parameter]
    public IReadOnlyList<ResumeLanguage> Languages { get; set; } = [];

    /// <summary>Told the languages with one added or taken out, when the page is edited.</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<ResumeLanguage>> LanguagesChanged { get; set; }

    /// <summary>Where the photo is served from; with none, there's no photo.</summary>
    [Parameter]
    public string? Photo { get; set; }

    /// <summary>The page's line on where he'd work.</summary>
    [Parameter]
    public string? Availability { get; set; }

    /// <summary>
    /// Told the top with one thing changed, when the page is edited. With nothing listening, the hero is plain words
    /// and its links lead where they say.
    /// </summary>
    [Parameter]
    public EventCallback<ResumeBasics> BasicsChanged { get; set; }

    /// <summary>Whether the hero is being edited.</summary>
    private bool Editable => BasicsChanged.HasDelegate;

    /// <summary>Where he lives, or an empty place when the CV says nothing yet.</summary>
    private ResumeLocation Place => Basics.Location ?? new ResumeLocation();

    /// <summary>The listener for his name, which is never left out: an emptied one is blank.</summary>
    private EventCallback<string?> NameChanged => Words(name => Basics with { Name = name ?? "" });

    /// <summary>The listener for the summary.</summary>
    private EventCallback<string?> SummaryChanged => Words(summary => Basics with { Summary = summary });

    /// <summary>The listener for the town, the province kept.</summary>
    private EventCallback<string?> TownChanged => Words(town => Basics with { Location = Place with { City = town } });

    /// <summary>The listener for the province, the town kept.</summary>
    private EventCallback<string?> ProvinceChanged =>
        Words(province => Basics with { Location = Place with { Region = province } });

    /// <summary>The listener for his email.</summary>
    private EventCallback<string?> EmailChanged => Words(email => Basics with { Email = email });

    /// <summary>A listener for some words of the top, there exactly when someone listens for the top.</summary>
    private EventCallback<string?> Words(Func<string?, ResumeBasics> changed) => BasicsChanged.Of(this, changed);

    /// <summary>The link whose form is open, if one is and it is still in the list.</summary>
    private ResumeProfile? OpenLink => _open is { } at && at < Basics.Profiles.Count ? Basics.Profiles[at] : null;

    /// <summary>What the open link is called, for its form: link 2.</summary>
    private string OpenLabel => $"link {_open + 1}";

    /// <summary>While the page is edited a link leads nowhere, so a click on it doesn't leave the page.</summary>
    private string? AddressOf(ResumeProfile link) => Editable ? null : link.Url;

    /// <summary>Opens a link's form.</summary>
    private void Open(int at) => _open = at;

    /// <summary>Closes the form, its edits dropped.</summary>
    private void Close() => _open = null;

    /// <summary>The open link is kept with its edits, in its place, and its form closes.</summary>
    private Task KeepLinkAsync(ResumeProfile link)
    {
        int at = _open ?? 0;
        _open = null;
        return LinksAsync(Basics.Profiles.Replaced(at, link));
    }

    /// <summary>A link moves a step along the row. An open form closes, since its place has changed.</summary>
    private Task MoveLinkAsync(int at, int step)
    {
        _open = null;
        return LinksAsync(Basics.Profiles.Moved(at, step));
    }

    /// <summary>A link is taken out.</summary>
    private Task RemoveLinkAsync(int at)
    {
        _open = null;
        return LinksAsync(Basics.Profiles.Without(at));
    }

    /// <summary>One more link, blank, at the end of the row, with its form open to fill it in.</summary>
    private Task AddLinkAsync()
    {
        _open = Basics.Profiles.Count;
        return LinksAsync(Basics.Profiles.Added(Blank));
    }

    /// <summary>The top with these links.</summary>
    private Task LinksAsync(IReadOnlyList<ResumeProfile> links) =>
        BasicsChanged.InvokeAsync(Basics with { Profiles = links });

    /// <summary>A link's network in its form's draft: LinkedIn, GitHub.</summary>
    private static Task NetworkAsync(EditedItem<ResumeProfile> link, string? network) =>
        link.Changed.InvokeAsync(link.Item with { Network = network ?? "" });

    /// <summary>What he is called there, shown on the link's button.</summary>
    private static Task NameOnItAsync(EditedItem<ResumeProfile> link, string? name) =>
        link.Changed.InvokeAsync(link.Item with { Username = name });

    /// <summary>The address behind the button.</summary>
    private static Task AddressAsync(EditedItem<ResumeProfile> link, string? address) =>
        link.Changed.InvokeAsync(link.Item with { Url = address ?? "" });

    /// <summary>The line under the summary: the town and province, the languages, and where he'd work.</summary>
    private string Where
    {
        get
        {
            string?[] place = [Basics.Location?.City, Basics.Location?.Region];
            string?[] parts = [string.Join(", ", place.OfType<string>()), .. TheRest];
            return string.Join(" · ", parts.Where(part => !string.IsNullOrEmpty(part)));
        }
    }

    /// <summary>What follows the place on that line: the languages he speaks, and where he'd work.</summary>
    private IEnumerable<string> TheRest
    {
        get
        {
            string?[] rest = [string.Join(" and ", Languages.Select(language => language.Language)), Availability];
            return rest.OfType<string>().Where(part => part.Length > 0);
        }
    }

    /// <summary>Where he'd work, as it is written after the languages when the line's pieces are drawn apart.</summary>
    private string AfterTheLanguages => string.IsNullOrEmpty(Availability) ? "" : $" · {Availability}";

    /// <summary>The listener for the languages, retyped as one line with commas between them.</summary>
    private EventCallback<string?> LanguagesTyped => LanguagesChanged.Of<IReadOnlyList<ResumeLanguage>, string?>(
        this, typed => ResumeLanguages.Typed(typed, Languages));

    /// <summary>A profile's button: LinkedIn's and WhatsApp's in their own look, the rest outlined.</summary>
    /// <remarks>A switch on the network, one arm per look (ADR 0051).</remarks>
    private static string ButtonOf(ResumeProfile profile) => profile.Network switch
    {
        LinkedIn => "btn btn-linkedin",
        WhatsApp => "btn btn-whatsapp",
        _ => "btn btn-line"
    };

    /// <summary>
    /// A profile's words: his number on WhatsApp, as a person would look for it; else the network, with his name on
    /// it when there is one.
    /// </summary>
    /// <remarks>A switch on the shape, one arm per kind of label (ADR 0051).</remarks>
    private string LabelOf(ResumeProfile profile) => profile switch
    {
        { Network: WhatsApp } when Basics.Phone is { } phone => phone,
        { Username: { } name } => $"{profile.Network} · {name}",
        _ => profile.Network
    };
}
