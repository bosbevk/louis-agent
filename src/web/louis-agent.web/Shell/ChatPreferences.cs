using LouisAgent.Web.Text;
using Microsoft.JSInterop;

namespace LouisAgent.Web.Shell;

/// <summary>What this browser remembers about the person: where they are, and whether the page may move.</summary>
/// <remarks>
/// Presentation Model (Fowler): the page's settings keep their state and rules here, and the components only draw them.
/// The script keeps the values, so nothing leaves the browser until a new chat sends the place.
/// </remarks>
/// <seealso href="https://martinfowler.com/eaaDev/PresentationModel.html">Presentation Model (Fowler)</seealso>
public sealed class ChatPreferences(IJSRuntime js, ChatText text)
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
        Location = await js.InvokeAsync<string?>("chatUi.loadLocation");
        Motion = await js.InvokeAsync<bool>("chatUi.motion");
        ReducedMotion = await js.InvokeAsync<bool>("chatUi.reducedMotion");
    }

    /// <summary>Keeps the typed place; an empty one clears it.</summary>
    public async Task SaveLocationAsync(string? value)
    {
        value = value?.Trim();
        Location = string.IsNullOrEmpty(value) ? null : value;
        await js.InvokeVoidAsync("chatUi.saveLocation", Location);
    }

    /// <summary>Keeps whether the page may move, and tells the page.</summary>
    public async Task SaveMotionAsync(bool motion)
    {
        Motion = motion;
        await js.InvokeVoidAsync("chatUi.setMotion", Motion);
    }

    /// <summary>Asks the browser where we are and keeps the place; the answer is what to tell the person.</summary>
    public async Task<string> UseMyLocationAsync()
    {
        Locating = true;
        try
        {
            var found = await js.InvokeAsync<LocatedPlace>("chatUi.locate");
            if (found.Place is not { Length: > 0 } place) return found.Error ?? text.LocationUnknown;

            Location = place;
            await js.InvokeVoidAsync("chatUi.saveLocation", place);
            return string.Format(text.LocationFound, place);
        }
        finally
        {
            Locating = false;
        }
    }
}
