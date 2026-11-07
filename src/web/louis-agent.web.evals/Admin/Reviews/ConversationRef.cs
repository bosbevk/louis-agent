namespace LouisAgent.Web.Evals.Admin.Reviews;

/// <summary>Which conversation of which run (F18 step 6a.3): what a grade is of.</summary>
public sealed record ConversationRef(string Run, string Mode, int Repeat, string Task);
