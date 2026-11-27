using LouisAgent.Core.Agent.EngineTools.ToolSearch;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Tests.Agent.EngineTools.ToolSearch;

/// <summary>
/// FindTool (F97.3) searches the tools the running chat sends by the words of what the model wants done: the best
/// matches first, five at most, never itself, and none when no word matches.
/// </summary>
public class FindToolTests
{
    /// <summary>A tool of the chat, made from its name and what it does.</summary>
    private static AIFunction Tool(string name, string description) =>
        AIFunctionFactory.Create((string value) => value, name, description);

    /// <summary>The chat's tools: a few git, file and build tools, and the search itself.</summary>
    private static readonly IReadOnlyList<AITool> ChatTools =
    [
        Tool("FindTool", "Finds the tools that do what you describe."),
        Tool("ShowCommit", "Show the last commit, or one by its id: its message, author and changes (git show)."),
        Tool("GetLog", "List the recent commits, newest first (git log)."),
        Tool("GetStatus", "Show which files are changed, staged or new (git status)."),
        Tool("ReadWorkspaceFile", "Read a file in the workspace."),
        Tool("DotNetListProjects", "List the .NET projects in a solution or a folder (dotnet sln list)."),
        Tool("DotNetBuild", "Build a .NET project or solution (dotnet build).")
    ];

    /// <summary>The search over the chat's tools.</summary>
    private static ToolSearchTools Search() => new(() => ChatTools);

    /// <summary>The tool whose name and words fit best comes first.</summary>
    [TestCase("show me what the last commit changed", "ShowCommit")]
    [TestCase("list the .NET projects in this folder", "DotNetListProjects")]
    [TestCase("read a file", "ReadWorkspaceFile")]
    [TestCase("which files are staged", "GetStatus")]
    public void TheBestMatch_ComesFirst(string query, string best) =>
        Assert.That(Search().FindTool(query).Names[0], Is.EqualTo(best));

    /// <summary>Five at most, and never the search itself, however many match.</summary>
    [Test]
    public void AtMostFive_AndNeverItself()
    {
        var found = Search().FindTool("find the tools to show list read build files commits projects");

        Assert.Multiple(() =>
        {
            Assert.That(found.Names, Has.Count.EqualTo(ToolSearchTools.MostFound));
            Assert.That(found.Names, Does.Not.Contain("FindTool"));
        });
    }

    /// <summary>Words no tool has find nothing, rather than the closest poor match.</summary>
    [Test]
    public void WordsNoToolHas_FindNothing() =>
        Assert.That(Search().FindTool("zebra giraffe").Names, Is.Empty);
}
