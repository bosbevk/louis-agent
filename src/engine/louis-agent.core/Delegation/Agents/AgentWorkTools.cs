using System.ComponentModel;
using LouisAgent.Composition.Agents;
using LouisAgent.Composition.Messages;
using LouisAgent.Core.Delegation.Messages;
using LouisAgent.Core.Sessions.SubSessions;

namespace LouisAgent.Core.Delegation.Agents;

/// <summary>
/// Part of the <c>agents</c> toolset (F127.3.1): reads back what an agent asked in this chat did, step by step, so the
/// asker can say what happened and why when the line under an answer isn't enough.
/// </summary>
/// <remarks>
/// Only a conversation this chat keeps can be read, so who may ask whom was settled when it was asked. The text is
/// held to the toolsets' usual cap with the oldest part cut (Louis's pick A): what is asked about is mostly the latest
/// work, and the result stays in the asker's history for every later turn.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0005-guard-the-context.md">ADR 0005: guard the context</seealso>
public sealed class AgentWorkTools
{
    /// <summary>The most it gives back: the toolsets' usual cap, the latest part kept.</summary>
    public const int MaxChars = 12_000;

    /// <summary>What the agent at an address did in this chat, as text, without its thinking.</summary>
    /// <remarks>Guard clauses (Fowler), one per reason there is nothing to read.</remarks>
    [Description(
        "Reads what an agent you asked in this chat did, step by step: what it said, each tool it called with what " +
        "it sent, and what came back, without its thinking. The latest 12,000 characters when it is longer.")]
    public string ReadAgentWork(
        [Description("The address you asked it at, as in AskAgent.")]
        string address)
    {
        if (!AgentAddress.TryParse(address, out var parsed))
            return MessageText.Format(SubSessionToolMessage.AddressInvalid, new { Address = address });
        if (SubSessionRegistry.Current is not { } kept || Asked(kept) is not { Count: > 0 } asked)
            return MessageText.Format(SubSessionToolMessage.AgentWorkNoneAsked);
        if (AgentLine.Pick(kept, parsed) is not { } open)
            return MessageText.Format(SubSessionToolMessage.AgentWorkNotAsked, new
            {
                Address = parsed.ToString(),
                Asked = string.Join(", ", asked)
            });

        return Latest(Retell(open.Work));
    }

    /// <summary>The addresses this chat has asked, oldest first.</summary>
    private static List<string> Asked(SubSessionRegistry kept) =>
    [
        .. kept.Open.Where(open => open is { Kind: OpenSubSession.AgentKind, Address: not null })
            .Select(open => open.Address!.ToString())
    ];

    /// <summary>The steps as lines, each answer numbered from the first.</summary>
    private static string Retell(AgentWork work)
    {
        var lines = new List<string>();
        int answer = 0;
        foreach (var step in work.Steps())
            lines.Add(step.Kind == AgentWork.StepKind.Began
                ? MessageText.Format(SubSessionToolMessage.AgentWorkBegan, new { Number = ++answer })
                : Line(step));
        return string.Join("\n", lines);
    }

    /// <summary>One step as a line.</summary>
    /// <remarks>A switch on the shape (ADR 0051), one arm per kind of step.</remarks>
    private static string Line(AgentWork.Step step) => step switch
    {
        { Kind: AgentWork.StepKind.Said } => MessageText.Format(SubSessionToolMessage.AgentWorkSaid,
            new { step.Text }),
        { Kind: AgentWork.StepKind.Called } => MessageText.Format(SubSessionToolMessage.AgentWorkCalled,
            new
            {
                step.Tool,
                Input = step.Text
            }),
        { Failed: true } => MessageText.Format(SubSessionToolMessage.AgentWorkFailed,
            new
            {
                Tool = step.Tool ?? "?",
                step.Text
            }),
        _ => MessageText.Format(SubSessionToolMessage.AgentWorkReturned,
            new
            {
                Tool = step.Tool ?? "?",
                step.Text
            })
    };

    /// <summary>The text whole, or its latest part under a line that says how much was cut.</summary>
    private static string Latest(string text) =>
        text.Length <= MaxChars
            ? text
            : MessageText.Format(SubSessionToolMessage.AgentWorkCut, new { Cut = text.Length - MaxChars }) + "\n" +
              text[^MaxChars..];
}
