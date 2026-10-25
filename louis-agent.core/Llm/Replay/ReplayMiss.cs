namespace LouisAgent.Core.Llm.Replay;

using LouisAgent.Core.Llm.Messages;
using LouisAgent.Core.Messages;

/// <summary>
/// What a replay says when the model's answer isn't recorded (F18 step 4e): the first part of the request that changed, against the
/// recording that matches it longest, in the order model, system prompt, tools, settings, messages (ADR 0005: a failure carries its
/// reason). Pure functions of two asks, so the cache client only throws what this returns.
/// </summary>
public static class ReplayMiss
{
    private enum Part { Model, System, Tools, Settings, Messages, Nothing }

    /// <summary>Where a request leaves a recording: the part, the position inside it, and the text. Later means a longer match.</summary>
    private sealed record Difference(Part Part, int Position, LlmErrorMessage Message, object Args);

    /// <summary>The exception text for a request with no recorded answer in <paramref name="trial"/>, given the model asks it does hold.</summary>
    public static string Describe(string trial, ModelAsk requested, IReadOnlyList<ModelAsk> recorded)
    {
        if (recorded.Count == 0) return MessageText.Format(LlmErrorMessage.ModelReplayNoAnswers, new { Trial = trial });

        Difference closest = recorded.Select(r => First(requested, r)).MaxBy(d => (d.Part, d.Position))!;
        string change = MessageText.Format(closest.Message, closest.Args);
        return MessageText.Format(LlmErrorMessage.ModelReplayMiss, new { Trial = trial, Change = change });
    }

    private static Difference First(ModelAsk now, ModelAsk was)
    {
        if (now.Model != was.Model)
            return new(Part.Model, 0, LlmErrorMessage.ReplayChangedModel, new { Recorded = was.Model, Requested = now.Model });
        if (now.System != was.System) return new(Part.System, 0, LlmErrorMessage.ReplayChangedSystem, new { });
        return FirstToolDifference(now.Tools, was.Tools)
            ?? (now.Settings != was.Settings ? new Difference(Part.Settings, 0, LlmErrorMessage.ReplayChangedSettings, new { }) : null)
            ?? FirstMessageDifference(now.Messages, was.Messages)
            ?? new Difference(Part.Nothing, 0, LlmErrorMessage.ReplayNothingChanged, new { });
    }

    private static Difference? FirstToolDifference(IReadOnlyList<ModelAsk.ToolHash> now, IReadOnlyList<ModelAsk.ToolHash> was)
    {
        for (int i = 0; i < now.Count; i++)
        {
            ModelAsk.ToolHash? same = was.FirstOrDefault(t => t.Name == now[i].Name);
            if (same is null) return new(Part.Tools, i, LlmErrorMessage.ReplayAddedTool, new { now[i].Name });
            if (same.Hash != now[i].Hash) return new(Part.Tools, i, LlmErrorMessage.ReplayChangedTool, new { now[i].Name });
        }
        foreach (ModelAsk.ToolHash gone in was.Where(t => now.All(n => n.Name != t.Name)))
            return new(Part.Tools, now.Count, LlmErrorMessage.ReplayGoneTool, new { gone.Name });

        return now.Select(t => t.Name).SequenceEqual(was.Select(t => t.Name))
            ? null
            : new(Part.Tools, now.Count, LlmErrorMessage.ReplayReorderedTools, new { });
    }

    private static Difference? FirstMessageDifference(IReadOnlyList<ModelAsk.MessageHash> now, IReadOnlyList<ModelAsk.MessageHash> was)
    {
        for (int i = 0; i < Math.Max(now.Count, was.Count); i++)
        {
            if (i >= was.Count) return new(Part.Messages, i, LlmErrorMessage.ReplayAddedMessage, new { Number = i + 1, now[i].Role });
            if (i >= now.Count) return new(Part.Messages, i, LlmErrorMessage.ReplayGoneMessage, new { Number = i + 1, was[i].Role });
            if (now[i] != was[i]) return new(Part.Messages, i, LlmErrorMessage.ReplayChangedMessage, new { Number = i + 1, now[i].Role });
        }
        return null;
    }
}
