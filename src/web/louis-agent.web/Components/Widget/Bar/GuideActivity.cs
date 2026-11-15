namespace LouisAgent.Web.Components.Widget.Bar;

/// <summary>
/// Whether the site's guide is talking right now (F134.4). The guide's bar says so as an answer starts and ends, and
/// whatever plays on the page beside it, such as a showroom's recorded demo, waits while it talks, so the visitor
/// follows one voice at a time.
/// </summary>
/// <remarks>
/// Observer, as <see cref="Door.AgentWindows"/> and <see cref="Door.GuideDoor"/> are: one small shared object the
/// showroom registers once; the bar tells it, and a demo listens. The bar sits in the layout and a demo in the page,
/// so a parameter can't reach from one to the other, and neither has to know the other is there.
/// </remarks>
public sealed class GuideActivity
{
    /// <summary>Whether the guide is answering.</summary>
    public bool Talking { get; private set; }

    /// <summary>Raised when the guide starts or stops talking.</summary>
    public event Action? Changed;

    /// <summary>Says whether the guide is talking; only a change is told.</summary>
    public void Set(bool talking)
    {
        if (talking == Talking) return;
        Talking = talking;
        Changed?.Invoke();
    }
}
