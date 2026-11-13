using LouisAgent.Site.Resumes;
using LouisAgent.Web.Components.Editing;
using LouisAgent.Web.Components.Editing.Inline;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Site.Components.Cv;

/// <summary>
/// The CV's facts row (F142): four numbers in a row, two by two on a phone, none when the file has none. When
/// someone listens for a change (the admin's editor), each fact is changed where it stands and the row is a list
/// to add to, take from and put in order. It never changes the facts it was given. It hands back a new list.
/// </summary>
public partial class ResumeFacts
{
    /// <summary>A fact to fill in.</summary>
    private static readonly ResumeFact Blank = new("", "");

    /// <summary>The facts, in the file's order.</summary>
    [Parameter]
    public IReadOnlyList<ResumeFact> Facts { get; set; } = [];

    /// <summary>Told the facts with one thing changed. With nothing listening, the row is plain words.</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<ResumeFact>> FactsChanged { get; set; }

    /// <summary>Whether the row is being edited.</summary>
    private bool Editable => FactsChanged.HasDelegate;

    /// <summary>The listener for a fact's figure, which is never left out: an emptied one is blank.</summary>
    private EventCallback<string?> FigureChanged(int at) => FactsChanged.Of<IReadOnlyList<ResumeFact>, string?>(
        this, figure => Facts.Replaced(at, Facts[at] with { Value = figure ?? "" }));

    /// <summary>The listener for the line under a figure.</summary>
    private EventCallback<string?> SayingChanged(int at) => FactsChanged.Of<IReadOnlyList<ResumeFact>, string?>(
        this, saying => Facts.Replaced(at, Facts[at] with { Label = saying ?? "" }));

    /// <summary>A fact moves a step along the row.</summary>
    private Task MoveAsync(int at, int step) => FactsChanged.InvokeAsync(Facts.Moved(at, step));

    /// <summary>A fact is taken out.</summary>
    private Task RemoveAsync(int at) => FactsChanged.InvokeAsync(Facts.Without(at));

    /// <summary>One more fact, blank, at the end of the row.</summary>
    private Task AddAsync() => FactsChanged.InvokeAsync(Facts.Added(Blank));
}
