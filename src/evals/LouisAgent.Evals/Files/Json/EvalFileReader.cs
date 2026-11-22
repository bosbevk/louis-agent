using System.Text.Json;
using LouisAgent.Composition.Messages;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Messages.Holes.Files;

namespace LouisAgent.Evals.Files.Json;

/// <summary>
/// Reads an eval file's text into its records, strictly, and refuses a wrong file once, at its first fault, saying
/// where and why: a fault of shape as the serializer found it, with its line, then a fault of meaning a shape can't
/// say, as the file is walked in order.
/// </summary>
/// <remarks>
/// Fail fast, with guard clauses: the first fault stops the read (Louis's pick, 2026-11-22, "Stop at the first
/// fault"), chosen over collecting every fault (Fowler's Notification) and over a schema validator package, since the
/// schema already shows most faults in the editor while the file is written. The place costs no code: the serializer
/// knows where it is, and puts that on the exception even when a converter refused the words.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0134-an-eval-is-one-file-of-conversations.md">ADR 0134: an eval is one file of conversations</seealso>
public static class EvalFileReader
{
    /// <summary>
    /// Where the serializer's own message starts telling the place, which the refusal tells once itself.
    /// </summary>
    private const string SerializerPlace = " Path: ";

    /// <summary>The eval the text holds, or an <see cref="EvalFileException"/> at its first fault.</summary>
    public static EvalFile Read(string json)
    {
        var file = Deserialize(json);
        Check(file);
        return file;
    }

    /// <summary>The file as its records, or the fault of shape the serializer found.</summary>
    private static EvalFile Deserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<EvalFile>(json, EvalJson.Options) ?? throw new JsonException();
        }
        catch (JsonException fault)
        {
            string place = fault.Path ?? "$";
            var ofShape = new FaultOfShapeHoles(place, (fault.LineNumber ?? 0) + 1, ReasonOf(fault.Message));
            throw new EvalFileException(place, MessageText.Format(EvalFileMessage.FaultOfShape, ofShape), fault);
        }
    }

    /// <summary>The serializer's reason, without the place it tells on its own messages.</summary>
    private static string ReasonOf(string message)
    {
        int place = message.IndexOf(SerializerPlace, StringComparison.Ordinal);
        return (place < 0 ? message : message[..place]).Trim();
    }

    /// <summary>
    /// What a shape can't say: at least one conversation, and each conversation right, in the file's order.
    /// </summary>
    private static void Check(EvalFile file)
    {
        if (file.Conversations.Count == 0) throw Refused("$.conversations", EvalFileMessage.NoConversations);

        var ids = new HashSet<string>(StringComparer.Ordinal);
        for (int at = 0; at < file.Conversations.Count; at++)
            CheckConversation(file.Conversations[at], $"$.conversations[{at}]", ids);
    }

    /// <summary>
    /// One conversation: an id used once, a turn at least, each turn right, a page that is an address, and something
    /// checked, since a conversation that checks nothing would pass whatever the agent said.
    /// </summary>
    private static void CheckConversation(Conversation conversation, string place, HashSet<string> ids)
    {
        if (string.IsNullOrWhiteSpace(conversation.Id)) throw Refused($"{place}.id", EvalFileMessage.NoId);

        var usedTwice = new IdUsedTwiceHoles(conversation.Id);
        if (!ids.Add(conversation.Id)) throw Refused($"{place}.id", EvalFileMessage.IdUsedTwice, usedTwice);

        if (conversation.Turns.Count == 0) throw Refused($"{place}.turns", EvalFileMessage.NoTurns);
        for (int at = 0; at < conversation.Turns.Count; at++)
            CheckTurn(conversation.Turns[at], $"{place}.turns[{at}]");

        var notAPage = new PageNotAnAddressHoles(conversation.Page ?? "");
        if (conversation.Page is { } page && !page.StartsWith('/'))
            throw Refused($"{place}.page", EvalFileMessage.PageNotAnAddress, notAPage);

        bool checks = conversation.Turns.Any(turn => turn.Facts.Count > 0 || turn.Criteria.Count > 0);
        if (!checks) throw Refused(place, EvalFileMessage.ChecksNothing);
    }

    /// <summary>One turn: it asks something, and no criterion is empty.</summary>
    private static void CheckTurn(Turn turn, string place)
    {
        if (string.IsNullOrWhiteSpace(turn.Ask)) throw Refused($"{place}.ask", EvalFileMessage.EmptyAsk);

        for (int at = 0; at < turn.Criteria.Count; at++)
            if (string.IsNullOrWhiteSpace(turn.Criteria[at]))
                throw Refused($"{place}.criteria[{at}]", EvalFileMessage.EmptyCriterion);
    }

    /// <summary>A fault of meaning, refused at its place in the message's words, for the caller to throw.</summary>
    /// <remarks>
    /// An exception builder method (Microsoft's .NET design guidelines): it returns the exception, so the stack trace
    /// starts where it is thrown. Named <c>Refused</c>, as the repo's other builders are (Louis's pick, F126.2).
    /// </remarks>
    private static EvalFileException Refused(string place, EvalFileMessage reason, object? holes = null)
    {
        var ofMeaning = new FaultOfMeaningHoles(place, MessageText.Format(reason, holes));
        return new EvalFileException(place, MessageText.Format(EvalFileMessage.FaultOfMeaning, ofMeaning));
    }
}
