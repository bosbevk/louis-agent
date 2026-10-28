using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Api.Evals.Runs.Protocol;
using LouisAgent.Api.Evals.Store;

namespace LouisAgent.Api.Admin.Replies;

/// <summary>Builds a note's row, and a conversation's key both ways between the contract and the store.</summary>
/// <remarks>
/// Replace Constructor with Factory Function (Fowler), as C# 14 static extension members, for the reason
/// <c>AdminReplies</c> gives: the records live in the contract, which can't see the store's note or key.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
internal static class NoteReplies
{
    /// <summary>A note's row copies the stored note, its conversation's key with it.</summary>
    extension(NoteRow)
    {
        /// <summary>The row for one stored note.</summary>
        public static NoteRow Of(EvalDocuments.RunNote note) =>
            new(note.Text, note.At, note.Conversation is { } key ? ConversationKeyRow.Of(key) : null);
    }

    /// <summary>A conversation's key, as the contract spells it.</summary>
    extension(ConversationKeyRow)
    {
        /// <summary>The row for a stored conversation's key.</summary>
        public static ConversationKeyRow Of(EvalProtocol.ConversationKey key) =>
            new(key.Mode, key.Repeat, key.Task);
    }

    /// <summary>The store's key for a conversation the portal named.</summary>
    public static EvalProtocol.ConversationKey ToKey(this ConversationKeyRow row) =>
        new(row.Mode, row.Repeat, row.Task);
}
