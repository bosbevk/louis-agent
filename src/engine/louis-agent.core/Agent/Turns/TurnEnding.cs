using LouisAgent.Composition.Tools;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Agent.Turns;

/// <summary>
/// When a tool ends the turn: it is marked <see cref="EndsTurnAttribute"/> (a tool that asks the person, such as a
/// choice or a form) and it worked. A refused call ended nothing, since the model will ask again.
/// </summary>
/// <remarks>
/// Extract Function (Fowler), shared: the tool-call guard stops the turn by this rule (FX138), and the turn runner
/// counts an answer closed by it (F127.4.3), so a form owes no choices beside it. One rule in one place means the turn
/// that stopped and the answer that counts as closed can't drift apart.
/// </remarks>
public static class TurnEnding
{
    /// <summary>Whether this tool's result ends the turn.</summary>
    public static bool Ends(AIFunction function, string result) =>
        function.UnderlyingMethod?.IsDefined(typeof(EndsTurnAttribute), inherit: true) == true &&
        !result.StartsWith("Error:", StringComparison.Ordinal);

    /// <summary>Whether an answer ended the turn with one of the turn's tools: a call made, and its result one that ends.</summary>
    public static bool Ended(IReadOnlyList<AIContent> answer, IEnumerable<AITool>? tools)
    {
        var functions = tools?.OfType<AIFunction>().ToList() ?? [];
        var results = answer.OfType<FunctionResultContent>().ToList();
        return answer.OfType<FunctionCallContent>().Any(call => EndedBy(call, functions, results));
    }

    /// <summary>Whether this one call ended the turn: its tool is one that ends, and its result says it worked.</summary>
    private static bool EndedBy(FunctionCallContent call, List<AIFunction> functions,
        List<FunctionResultContent> results)
    {
        var function = functions.FirstOrDefault(f => f.Name == call.Name);
        if (function is null) return false;
        return results.Any(result => result.CallId == call.CallId && Ends(function, result.Result?.ToString() ?? ""));
    }
}
