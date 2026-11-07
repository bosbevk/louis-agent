namespace LouisAgent.Web.Admin.Studio;

/// <summary>One run a try can replay: its id, sent to the API, and the line the picker shows for it.</summary>
public sealed record ReplayChoice(string Run, string Label);
