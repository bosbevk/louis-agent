using LouisAgent.Api.Contracts.Admin.Studio;
using LouisAgent.Api.Evals.Studio;
using LouisAgent.Choices;

namespace LouisAgent.Api.Admin.Replies;

/// <summary>Builds the studio's catalogue reply from the runner's lines, each kind by its wire name.</summary>
/// <remarks>
/// Replace Constructor with Factory Function (Fowler), as C# 14 static extension members, for the reason
/// <c>AdminReplies</c> gives: the records live in the contract, which can't see the runner's lines.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
internal static class StudioReplies
{
    /// <summary>The catalogue's reply.</summary>
    extension(CheckListReply)
    {
        /// <summary>The reply for what the runner listed: its checks, then its readers.</summary>
        public static CheckListReply Of(CheckListingResult listed) =>
            new([.. listed.Checks.Select(check => new CheckTypeRow(check.Type, check.Kind.Wire(), check.Parameters))],
                [.. listed.Readers.Select(reader => new ReaderRow(reader.Reader, reader.Kind.Wire()))]);
    }
}
