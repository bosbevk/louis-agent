namespace LouisAgent.Web.Components.Areas.Overview;

/// <summary>A tool a site's overview counts (F127.8), by its name and by what the overview calls it.</summary>
/// <param name="Tool">The tool's name, as the talks called it, such as SendLead.</param>
/// <param name="Name">What the overview calls the talks that called it, such as leads.</param>
public sealed record CountedTool(string Tool, string Name);
