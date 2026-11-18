using LouisAgent.Composition.Messages;
using LouisAgent.Web.Messages;
using LouisAgent.Web.Messages.Holes;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Tables;

/// <summary>
/// Things set side by side on a page (F161.1): what is compared across the top, each topic a row down the side with
/// one cell under each column. A row that doesn't fill its columns is refused, with which row and what it needs, so
/// a cell can never stand under the wrong column.
/// </summary>
public partial class ComparisonTable
{
    /// <summary>What is compared, across the top.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<string> Columns { get; set; } = [];

    /// <summary>The topics, each with one cell under each column.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<ComparisonRow> Rows { get; set; } = [];

    /// <summary>Refuses a row whose cells don't line up with the columns, before anything is drawn.</summary>
    protected override void OnParametersSet()
    {
        if (Rows.FirstOrDefault(row => row.Cells.Count != Columns.Count) is not { } unfilled) return;

        var unfilledRow = new ComparisonRowShortHoles(unfilled.Topic, unfilled.Cells.Count, Columns.Count);
        throw new InvalidOperationException(MessageText.Format(WebErrorMessage.ComparisonRowShort, unfilledRow));
    }
}
