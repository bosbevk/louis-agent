namespace LouisAgent.Web.Components.Staging.Windows;

/// <summary>
/// One pane of work in a window (F134.1): a heading over a few lines, such as the job an agent was given or what it
/// did. <see cref="WorkBody"/> draws them side by side.
/// </summary>
/// <param name="Heading">What the pane is, such as "The job".</param>
/// <param name="Lines">Its lines, in order.</param>
public sealed record WorkPane(string Heading, IReadOnlyList<string> Lines);
