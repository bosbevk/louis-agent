namespace LouisAgent.Evals.Puls.Tests;

/// <summary>
/// The ids of the routing dataset's tasks that these tests name more than once (ADR 0043, F18 step 7.5.4e). A task id is data in the dataset file, which keeps the text, so it is not an enum: it is
/// one constant per test project where it repeats. Some coincide with a toolset's name (the "web" task, the "numbers" task); they are the dataset's names for tasks, not the toolsets.
/// </summary>
public static class PulsTaskIds
{
    public const string Web = "web";
    public const string Files = "files";
    public const string Git = "git";
    public const string Numbers = "numbers";
    public const string Chat = "chat";
    public const string Cook = "cook";
}
