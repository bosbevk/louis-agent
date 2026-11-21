using LouisAgent.Web.Shell.Location;
using LouisAgent.Choices;
using LouisAgent.Web.Scripts;
using LouisAgent.Web.Text;

namespace LouisAgent.Web.Shell;

/// <summary>What this browser remembers about the person: where they are, and whether the page may move.</summary>
/// <remarks>
/// Presentation Model (Fowler): the page's settings keep their state and rules here, and the components only draw them.
/// The framework's preferences module keeps the values (CL31), so nothing leaves the browser until a new chat sends the
/// place.
/// </remarks>
/// <seealso href="https://martinfowler.com/eaaDev/PresentationModel.html">Presentation Model (Fowler)</seealso>
public sealed class ChatPreferences(JsModule preferences, ChatText text)
{
    /// <summary>Where the person said they are; null when they haven't.</summary>
    public string? Location { get; private set; }

    /// <summary>The browser is looking for the place.</summary>
    public bool Locating { get; private set; }

    /// <summary>The page shows animations although the system asks for less.</summary>
    public bool Motion { get; private set; }

    /// <summary>The system asks for less motion.</summary>
    public bool ReducedMotion { get; private set; }

    /// <summary>Reads what this browser kept.</summary>
    public async Task LoadAsync()
    {
        Location = await preferences.InvokeAsync<string?>("loadLocation");
        Motion = await preferences.InvokeAsync<bool>("motion");
        ReducedMotion = await preferences.InvokeAsync<bool>("reducedMotion");
    }

    /// <summary>Keeps the typed place; an empty one clears it.</summary>
    public async Task SaveLocationAsync(string? value)
    {
        value = value?.Trim();
        Location = string.IsNullOrEmpty(value) ? null : value;
        await preferences.InvokeVoidAsync("saveLocation", Location);
    }

    /// <summary>Keeps whether the page may move, and tells the page.</summary>
    public async Task SaveMotionAsync(bool motion)
    {
        Motion = motion;
        await preferences.InvokeVoidAsync("setMotion", Motion);
    }

    /// <summary>Asks the browser where we are and keeps the place; the answer is what to tell the person.</summary>
    public async Task<string> UseMyLocationAsync()
    {
        Locating = true;
        try
        {
            var found = await preferences.InvokeAsync<LocatedPlace>("locate", text.PlaceLanguages);
            if (found.Place is not { Length: > 0 } place) return Say(found.Error);

            Location = place;
            await preferences.InvokeVoidAsync("saveLocation", place);
            return string.Format(text.LocationFound, place);
        }
        finally
        {
            Locating = false;
        }
    }

    /// <summary>The site's words for the problem the script named, or the plain one when it named none.</summary>
    private string Say(string? problem)
    {
        if (!SettingChoice.TryParse(problem, out LocationProblem? named)) return text.LocationUnknown;

        return named switch
        {
            LocationProblem.NoBrowser => text.LocationNoBrowser,
            LocationProblem.Denied => text.LocationDenied,
            LocationProblem.NotNamed => text.LocationNotNamed,
            LocationProblem.Unreachable => text.LocationUnreachable,
            _ => text.LocationUnknown,
        };
    }
}
