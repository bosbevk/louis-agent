using System.Text.Json;
using LouisAgent.Orchestrator.Triage;

namespace LouisAgent.Orchestrator.Tests.Triage;

/// <summary>
/// Reading the triage agent's closing "DECISION: outcome - reason" line, and the line it becomes in decisions.jsonl.
/// </summary>
public class DecisionTests
{
    /// <summary>The line is found however the model dresses it: bold, a long dash, a colon, any case.</summary>
    [TestCase("Done.\nDECISION: fixed - commit abc1234 verified", "fixed", "commit abc1234 verified")]
    [TestCase("**DECISION:** ignored — expected 404", "ignored", "expected 404")]
    [TestCase("DECISION: Escalated: payments must check the refund", "escalated", "payments must check the refund")]
    [TestCase("DECISION: **failed** – tests still red\r\n", "failed", "tests still red")]
    public void Parse_ReadsTheClosingLine(string answer, string outcome, string reason)
    {
        var decision = Decision.Parse(answer);

        Assert.That(decision.Outcome, Is.EqualTo(outcome));
        Assert.That(decision.Reason, Is.EqualTo(reason));
    }

    /// <summary>If the model changed its mind, its last decision counts.</summary>
    [Test]
    public void Parse_TakesTheLastDecision()
    {
        const string answer = "DECISION: escalated - not sure\nOn second look:\nDECISION: ignored - expected 404";

        var decision = Decision.Parse(answer);

        Assert.That((decision.Outcome, decision.Reason), Is.EqualTo(("ignored", "expected 404")));
    }

    /// <summary>A line that names no outcome, or gives no reason, isn't a decision.</summary>
    [TestCase("I looked at it.")]
    [TestCase("DECISION: - nothing")]
    [TestCase("DECISION: fixed")]
    [TestCase("DECISION: fixed -   ")]
    public void Parse_WithoutADecisionLine_IsUnknown(string answer) =>
        Assert.That(Decision.Parse(answer).Outcome, Is.EqualTo("unknown"));

    /// <summary>Without a decision line, the reason is the answer itself, on one line, cut at 200 characters.</summary>
    [Test]
    public void Parse_WithoutADecisionLine_KeepsTheAnswerShort()
    {
        Assert.That(Decision.Parse(" I looked\nat it. ").Reason, Is.EqualTo("I looked at it."));
        Assert.That(Decision.Parse(new string('a', 250)).Reason, Is.EqualTo(new string('a', 200) + "..."));
    }

    /// <summary>decisions.jsonl uses snake_case keys, like fixes.jsonl and escalations.jsonl.</summary>
    [Test]
    public void Record_UsesSnakeCaseKeys_LikeTheOtherRecords()
    {
        var error = new ErrorEvent("abc123", DateTimeOffset.UnixEpoch, "order-service", "order-total", "1003",
            "System.Collections.Generic.KeyNotFoundException", "The given key 'SUMMER25' was not present", null);
        var used = new EventUsage(null, null, null);
        var at = new DateTimeOffset(2026, 10, 12, 8, 0, 0, TimeSpan.Zero);

        var record = DecisionRecord.Of(Decision.Parse("DECISION: fixed - verified"), error, used, "USD", at);
        string json = JsonSerializer.Serialize(record);

        Assert.That(json,
            Is.EqualTo(
                """{"error_id":"abc123","method":"order-total","outcome":"fixed","reason":"verified","at":"2026-10-12T08:00:00+00:00","usage":null,"triage_usage":null,"louis_agent_usage":null}"""));
    }
}
