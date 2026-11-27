using System.ComponentModel;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Agent.EngineTools.ToolSearch;

/// <summary>
/// The engine's own search for the tools a chat keeps out of sight until they are needed (F97.3): it scores the tools
/// the running chat sends by the words of what the model wants done, and names the best five.
/// </summary>
/// <param name="tools">The tools the running chat sends, the turn's own list (FX4).</param>
public sealed class ToolSearchTools(Func<IReadOnlyList<AITool>> tools)
{
    /// <summary>The most tools one search names, Anthropic's own search's default.</summary>
    public const int MostFound = 5;

    /// <summary>The search's own name, which it never finds.</summary>
    private const string Self = nameof(FindTool);

    /// <summary>Finds the tools that do what the model describes, best first; none when no word fits.</summary>
    [Description("""
                 Finds the tools that do what you describe, among the ones not listed yet, and makes them callable by
                 their own names. Describe the job in a few plain words, such as "show the last commit" or "list the
                 projects in a folder".
                 """)]
    public FoundTools FindTool([Description("What you want done, in a few words.")] string query)
    {
        var words = ToolWords.Of(query);
        var best = tools()
            .Where(tool => tool.Name != Self)
            .Select(tool => ToolScore.Of(tool, words))
            .Where(score => score.Fits)
            .OrderByDescending(score => score.Covered)
            .ThenByDescending(score => score.Weight)
            .ThenBy(score => score.Name, StringComparer.Ordinal)
            .Take(MostFound);
        return new FoundTools([.. best.Select(score => score.Name)]);
    }
}
