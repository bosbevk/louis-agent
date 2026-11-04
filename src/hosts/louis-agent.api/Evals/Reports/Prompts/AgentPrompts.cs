using LouisAgent.Core.Usage.Records;
using static LouisAgent.Api.Evals.Store.EvalDocuments;

namespace LouisAgent.Api.Evals.Reports.Prompts;

/// <summary>
/// Sorts a run's ledger rows by who asked, and keeps each agent's prompt hashes per mode. The trend compares runs agent
/// by agent with these: a run where the cook wasn't called has fewer hashes, and that isn't a changed prompt.
/// </summary>
/// <remarks>
/// A composite key (the unattended run's finding 14, PoEAA's Identity Field over several columns): a prompt belongs to
/// a mode, an agent and the reason it asked. The agent is the row's task tag (<c>sub-session:{peer}</c>,
/// <c>minion:{role}</c>), and any other task is the main session; a judge or a summary asks with a prompt of its own,
/// so it gets its purpose after the agent's name and is only ever compared with itself.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0007-measure-every-request.md">ADR 0007: measure every request</seealso>
public static class AgentPrompts
{
    /// <summary>The name of the session the eval talks to, whatever scenario it is on.</summary>
    public const string Main = "main";

    /// <summary>The task tags a sub-session's rows carry, before the peer's or the role's name.</summary>
    private static readonly string[] ChildTags = ["sub-session:", "minion:"];

    /// <summary>
    /// Each agent's prompts in each mode of the run, its hashes sorted so two runs on the same prompts read the same.
    /// A ledger from before prompts were hashed tells us nothing, so that's unknown rather than an empty list.
    /// </summary>
    public static IReadOnlyList<AgentPrompt>? Of(string run, IReadOnlyList<UsageRecord> ofRun)
    {
        var hashed = ofRun.Where(r => r.PromptHash is not null).ToList();
        if (hashed.Count == 0) return null;

        return hashed
            .GroupBy(r => (Mode: ModeOf(run, r.Run), Agent: AgentOf(r)))
            .Select(g => new AgentPrompt(g.Key.Mode, g.Key.Agent,
                g.Select(r => r.PromptHash!).Distinct().Order(StringComparer.Ordinal).ToArray()))
            .OrderBy(p => p.Mode, StringComparer.Ordinal)
            .ThenBy(p => p.Agent, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// The agents in a mode whose prompts changed between two runs. Only an agent both runs called counts: one a run
    /// never called simply wasn't there. If either run didn't keep its agents' prompts, we can't tell.
    /// </summary>
    public static IReadOnlyList<string>? Changed(IReadOnlyList<AgentPrompt>? before, IReadOnlyList<AgentPrompt>? now,
        string mode)
    {
        if (before is null || now is null) return null;

        // The hashes are kept sorted, so two lists in the same order are the same set of prompts.
        var earlier = before.Where(p => p.Mode == mode).DistinctBy(p => p.Agent)
            .ToDictionary(p => p.Agent, p => p.Hashes);
        return now
            .Where(p => p.Mode == mode && earlier.TryGetValue(p.Agent, out var was) && !was.SequenceEqual(p.Hashes))
            .Select(p => p.Agent)
            .ToArray();
    }

    /// <summary>
    /// Who asked: the specialist or minion named in the task, else the main session; a request that isn't a turn (a
    /// judge, a summary) adds why it asked.
    /// </summary>
    private static string AgentOf(UsageRecord row)
    {
        string agent = row.Task is { } task && ChildTags.Any(t => task.StartsWith(t, StringComparison.Ordinal))
            ? task
            : Main;
        return row.Purpose is UsagePurpose.Turn or UsagePurpose.Continue ? agent : $"{agent} ({row.Purpose})";
    }

    /// <summary>
    /// The mode in the row's run tag, <c>{run}:{mode}:{repeat}</c>; a row tagged with the run alone has none.
    /// </summary>
    private static string? ModeOf(string run, string? tag)
    {
        if (tag is null || tag.Length <= run.Length + 1) return null;

        // What follows "{run}:" is "{mode}:{repeat}", the repeat a number: the mode is all before the last colon.
        string rest = tag[(run.Length + 1)..];
        int colon = rest.LastIndexOf(':');
        return colon < 0 ? rest : rest[..colon];
    }
}
