using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Bands;

/// <summary>
/// An entry in the page's section index for an element that isn't a band (F118): the hero, which has its own shape,
/// marks its place with <c>&lt;AreaMark Id="top" Label="Top" /&gt;</c>. It draws nothing and registers like a band.
/// </summary>
public partial class AreaMark : IDisposable
{
    /// <summary>The page's list of areas, from the frame.</summary>
    [CascadingParameter] public PageAreas? Areas { get; set; }

    /// <summary>The id of the element the index links to.</summary>
    [Parameter, EditorRequired] public string Id { get; set; } = "";

    /// <summary>The index's label for it.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = "";

    /// <summary>Puts the place on the list.</summary>
    protected override void OnParametersSet() => Areas?.Add(Id, Label);

    /// <summary>Takes it off when the page goes.</summary>
    public void Dispose() => Areas?.Remove(Id);
}
