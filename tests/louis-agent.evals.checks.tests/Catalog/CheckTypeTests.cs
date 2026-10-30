using System.Text.Json;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Checks.Tests.Catalog;

/// <summary>
/// A check type builds its check from the settings an eval file gives it, and turns away settings it can't read,
/// saying which check type and which field were wrong.
/// </summary>
public class CheckTypeTests
{
    private static readonly TimeLimitType Entry = new();

    /// <summary>Good settings come out as the check they describe, and the entry says what it is.</summary>
    [Test]
    public void BuildsItsCheck_FromItsParameters()
    {
        var check = Entry.Create(Json("""{"seconds": 30}"""), CheckContext.None);

        Assert.That(check, Is.TypeOf<TimeLimitCheck>());
        Assert.That(((TimeLimitCheck)check).Seconds, Is.EqualTo(30));
        Assert.That(Entry.Type, Is.EqualTo("time-limit"));
        Assert.That(Entry.Kind, Is.EqualTo(CheckKind.FixedRule));
        Assert.That(Entry.Parameters, Is.EqualTo(typeof(TimeLimitParameters)));
    }

    /// <summary>A field of the wrong type is refused, naming the check type and the field.</summary>
    [Test]
    public void AFieldOfTheWrongType_IsRefused()
    {
        var refusal =
            Assert.Throws<InvalidDataException>(() => Entry.Create(Json("""{"seconds": "x"}"""), CheckContext.None));

        Assert.That(refusal!.Message, Does.Contain("time-limit").And.Contain("seconds"));
    }

    /// <summary>A missing field is refused too, rather than quietly read as zero.</summary>
    [Test]
    public void AMissingField_IsRefused()
    {
        var refusal = Assert.Throws<InvalidDataException>(() => Entry.Create(Json("{}"), CheckContext.None));

        Assert.That(refusal!.Message, Does.Contain("time-limit").And.Contain("seconds"));
    }

    /// <summary>A field the record doesn't have is refused, so a typo can't pass unnoticed.</summary>
    [Test]
    public void AnUnknownField_IsRefused()
    {
        var refusal = Assert.Throws<InvalidDataException>(() =>
            Entry.Create(Json("""{"seconds": 30, "secnds": 30}"""), CheckContext.None));

        Assert.That(refusal!.Message, Does.Contain("time-limit").And.Contain("secnds"));
    }

    /// <summary>No settings at all (a JSON null) is refused, naming the check type.</summary>
    [Test]
    public void NoParameters_IsRefused()
    {
        var refusal = Assert.Throws<InvalidDataException>(() => Entry.Create(Json("null"), CheckContext.None));

        Assert.That(refusal!.Message, Does.Contain("time-limit"));
    }

    /// <summary>The settings exactly as an eval file would hold them.</summary>
    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();

    /// <summary>The test's settings: how many seconds a turn may take.</summary>
    private sealed record TimeLimitParameters(int Seconds);

    /// <summary>A check type for the test, built from <see cref="TimeLimitParameters"/>.</summary>
    private sealed class TimeLimitType() : CheckType<TimeLimitParameters>("time-limit", CheckKind.FixedRule)
    {
        /// <summary>Builds the test's check with the seconds it was given.</summary>
        protected override ICheck Create(TimeLimitParameters parameters, CheckContext context) =>
            new TimeLimitCheck(parameters.Seconds);
    }

    /// <summary>The check the test's type builds; it only keeps its seconds, so a test can read them back.</summary>
    private sealed class TimeLimitCheck(int seconds) : ICheck
    {
        /// <summary>The limit it was built with.</summary>
        public int Seconds => seconds;

        public string Name => "time-limit";
        public int Version => 1;
        public CheckKind Kind => CheckKind.FixedRule;
        public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>();
        public bool Required => true;

        /// <summary>Always passes: the tests here are about building the check, not grading.</summary>
        public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
            ValueTask.FromResult(Verdict.Pass(Name, Version, "built"));
    }
}
