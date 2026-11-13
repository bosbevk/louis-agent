using LouisAgent.Site.Resumes;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Site.Components.Cv;

/// <summary>
/// The CV's facts row (F142): four numbers in a row, two by two on a phone, none when the file has none.
/// </summary>
public partial class ResumeFacts
{
    /// <summary>The facts, in the file's order.</summary>
    [Parameter]
    public IReadOnlyList<ResumeFact> Facts { get; set; } = [];
}
