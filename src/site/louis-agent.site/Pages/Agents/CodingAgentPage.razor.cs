using LouisAgent.Web.Components.Recorded;
using LouisAgent.Web.Usage;

namespace LouisAgent.Site.Pages.Agents;

/// <summary>
/// The coding agent's page (F120): a visitor watches it fix the invoice matcher, recorded once and played back in the
/// framework's chat view, then gets the offer. Nothing runs while they watch, and the live chat stays closed (FX88).
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0108-the-public-watches-the-coding-agent-recorded.md">ADR 0108: the public watches the coding agent recorded</seealso>
public partial class CodingAgentPage
{
    /// <summary>
    /// The question the recorded session answered, word for word what <c>recordings/record.sh</c> asked; a test holds
    /// the two together, so a new recording with a new question changes both.
    /// </summary>
    public const string Question =
        "The bank payments are not matching their invoices, and the tests show it. " +
        "Find out why, fix the matcher, and run the tests again.";

    /// <summary>The recorded session: one turn, the question and its stream.</summary>
    private static readonly RecordedTurn[] Session = [new(Question, "recordings/coding-session.sse")];

    /// <summary>
    /// The bill in rand at the rate the ledger used on the day it was recorded, as its own display cost said
    /// (R0.10 for US$0.0062): a recording's price stays the price it was.
    /// </summary>
    private static readonly CurrencyChoice Rand = new("ZAR", "R", 16.5382m, "2026-11-06", "USD");
}
