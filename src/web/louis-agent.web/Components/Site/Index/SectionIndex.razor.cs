using LouisAgent.Web.Components.Site.Bands;
using LouisAgent.Web.Scripts;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace LouisAgent.Web.Components.Site.Index;

/// <summary>
/// The section index on the left of every page (F118): the areas the page's bands put on the frame's list, redrawn as
/// they come and go, in the order their sections stand on the page, with the one in view lit. The landing wrote this
/// list by hand before.
/// </summary>
/// <remarks>
/// Observer over <see cref="PageAreas"/>: the index listens to the list and redraws, so a band knows nothing of the
/// index. The order comes from the page, not from the list: a band inside a component (the hero, a tile strip)
/// registers after the page's own bands, so after each draw the module reads where each section stands and the list
/// is put in that order; a second draw finds nothing to change and stops.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0093-a-script-is-a-module-beside-its-component.md">ADR 0093: a script is a module beside its component</seealso>
public partial class SectionIndex : IAsyncDisposable
{
    /// <summary>The class the module puts on the link in view, which the styles beside this component light.</summary>
    private const string ActiveClass = "active";

    /// <summary>The page's areas, from the frame.</summary>
    [CascadingParameter] public PageAreas? Areas { get; set; }

    [Inject] private IJSRuntime JS { get; set; } = default!;

    private PageAreas? _watched;
    private JsModule? _module;

    /// <summary>The index's own module: the page's order and the link in view.</summary>
    private JsModule Module => _module ??= new JsModule(JS, FrameworkScripts.SectionIndex);

    /// <summary>Follows the list it is given: when an area comes or goes, the index redraws.</summary>
    protected override void OnParametersSet()
    {
        if (ReferenceEquals(_watched, Areas)) return;
        if (_watched is not null) _watched.Changed -= Redraw;
        _watched = Areas;
        if (_watched is not null) _watched.Changed += Redraw;
    }

    /// <summary>
    /// After every draw, marks the link in view (the page may have grown since the last mark, so a mark made on a
    /// shorter page is wrong now) and puts the list in the order the sections stand on the page.
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Areas is null) return;
        await Module.InvokeVoidAsync("attach", ActiveClass);

        // The browser's own order of the sections; a test's browser answers nothing, and the list keeps its order.
        string[] ids = Areas.Items.Select(entry => entry.Id).ToArray();
        string[]? onThePage = await Module.InvokeAsync<string[]?>("order", [ids]);
        if (onThePage is not null) Areas.Reorder(onThePage);
    }

    /// <summary>Stops listening and takes the spy down; a closed browser tab has no JS runtime to tell.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_watched is not null) _watched.Changed -= Redraw;
        if (_module is null) return;
        try
        {
            await _module.InvokeVoidAsync("detach");
        }
        catch (JSDisconnectedException)
        {
        }

        await _module.DisposeAsync();
    }

    private void Redraw() => InvokeAsync(StateHasChanged);
}
