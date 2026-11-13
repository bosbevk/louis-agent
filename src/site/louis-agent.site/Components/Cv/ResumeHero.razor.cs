using LouisAgent.Site.Resumes.Basics;
using LouisAgent.Site.Resumes.Skills;
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

    /// <summary>Who he is and how to reach him.</summary>
    [Parameter, EditorRequired]
    public ResumeBasics Basics { get; set; } = default!;

    /// <summary>The languages he speaks.</summary>
    [Parameter]
    public IReadOnlyList<ResumeLanguage> Languages { get; set; } = [];

    /// <summary>Where the photo is served from; with none, there's no photo.</summary>
    [Parameter]
    public string? Photo { get; set; }

    /// <summary>The page's line on where he'd work.</summary>
    [Parameter]
    public string? Availability { get; set; }

    /// <summary>The line under the summary: the town and province, the languages, and where he'd work.</summary>
    private string Where
    {
        get
        {
            string?[] place = [Basics.Location?.City, Basics.Location?.Region];
            string?[] parts =
            [
                string.Join(", ", place.OfType<string>()),
                string.Join(" and ", Languages.Select(language => language.Language)),
                Availability
            ];
            return string.Join(" · ", parts.Where(part => !string.IsNullOrEmpty(part)));
        }
    }

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
