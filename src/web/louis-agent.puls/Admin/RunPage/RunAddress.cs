using LouisAgent.Choices;
using LouisAgent.Puls.Admin.Explorer.Place;

namespace LouisAgent.Puls.Admin.RunPage;

/// <summary>A run's address (`admin/runs/{id}`), its tab, and on the conversations tab the explorer's place.</summary>
/// <remarks>
/// One place writes the address, so the tab bar, the summary's task links, the verdict's "to grade" link and the
/// explorer's moves can't drift apart in how they spell it.
/// </remarks>
public static class RunAddress
{
    /// <summary>The address of this run, opening on its summary.</summary>
    public static string Of(string run) => $"admin/runs/{Uri.EscapeDataString(run)}";

    /// <summary>The address of this run on this tab, at this place in the explorer if one is given.</summary>
    public static string Of(string run, RunTab tab, ExplorerPlace? place)
    {
        string address = $"{Of(run)}?tab={tab.Wire()}";
        return place is null ? address : $"{address}&{place.Query()}";
    }
}
