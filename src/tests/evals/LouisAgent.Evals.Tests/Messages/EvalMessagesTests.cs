using LouisAgent.Evals.Messages;
using LouisAgent.Testing.Messages;

namespace LouisAgent.Evals.Tests.Messages;

/// <summary>
/// The evals' texts keep their holes honest (ADR 0119): every member with holes names its record, and the record's
/// properties are the template's holes, no more and no fewer.
/// </summary>
public class EvalMessagesTests
{
    /// <summary>Each of the evals' message enums, with its holes records.</summary>
    [Test]
    public void EveryMessage_NamesItsHolesRecord_AndItMatches() =>
        Assert.That(MessageHoles.Problems([typeof(EvalFileMessage), typeof(FactMessage),
            typeof(JudgeMessage)]), Is.Empty);
}
