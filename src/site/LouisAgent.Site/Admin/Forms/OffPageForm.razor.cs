using LouisAgent.Site.Resumes.Basics;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Site.Admin.Forms;

/// <summary>
/// The form for what the CV holds and the page never draws (F142): the headline, the phone number, the website and
/// the country. Everything else is edited on the page itself, so only these need a box of their own. It never
/// changes what it was given: each edit is handed back as a new copy with that one thing changed.
/// </summary>
public partial class OffPageForm
{
    /// <summary>The top of the CV as the draft holds it.</summary>
    [Parameter, EditorRequired]
    public ResumeBasics Basics { get; set; } = default!;

    /// <summary>Told the top with one thing changed.</summary>
    [Parameter]
    public EventCallback<ResumeBasics> BasicsChanged { get; set; }

    /// <summary>Where he lives, or an empty place when the CV says nothing yet.</summary>
    private ResumeLocation Location => Basics.Location ?? new ResumeLocation();

    /// <summary>The line that sums him up, for a search result or an export.</summary>
    private Task HeadlineAsync(string? headline) => BasicsChanged.InvokeAsync(Basics with { Label = headline });

    /// <summary>His phone number.</summary>
    private Task PhoneAsync(string? phone) => BasicsChanged.InvokeAsync(Basics with { Phone = phone });

    /// <summary>His own website.</summary>
    private Task WebsiteAsync(string? website) => BasicsChanged.InvokeAsync(Basics with { Url = website });

    /// <summary>
    /// The country, by its two letters, the town and the province kept. A place with nothing left in it is taken
    /// out of the CV whole.
    /// </summary>
    private Task CountryAsync(string? code)
    {
        var place = Location with { CountryCode = code };
        return BasicsChanged.InvokeAsync(Basics with { Location = place == new ResumeLocation() ? null : place });
    }
}
