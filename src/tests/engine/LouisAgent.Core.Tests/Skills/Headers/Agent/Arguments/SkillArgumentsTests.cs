using System.Text.Json;
using LouisAgent.Core.Skills.Headers.Agent;
using LouisAgent.Core.Skills.Headers.Agent.Arguments;

namespace LouisAgent.Core.Tests.Skills.Headers.Agent.Arguments;

/// <summary>
/// F97.1 step 3: a call's arguments are checked against the skill's parameters before its script runs, by our own
/// code (Louis's pick, no package): each required one there, none it doesn't take, and each value of its type. Every
/// problem is named, so the model can fix the call in one go.
/// </summary>
public class SkillArgumentsTests
{
    /// <summary>A skill that takes a required text, a whole number, a yes or no, and a list.</summary>
    private static readonly SkillParameters Parameters = new(
        JsonDocument.Parse("{}").RootElement,
        new Dictionary<string, SkillParameterType>
        {
            ["text"] = SkillParameterType.String,
            ["limit"] = SkillParameterType.Integer,
            ["loud"] = SkillParameterType.Boolean,
            ["words"] = SkillParameterType.Array
        },
        ["text"]);

    /// <summary>A good call has no problems, the optional ones left out or given.</summary>
    [TestCase("""{ "text": "a b" }""")]
    [TestCase("""{ "text": "a b", "limit": 3, "loud": true, "words": ["a"] }""")]
    public void AGoodCall_HasNoProblems(string arguments) =>
        Assert.That(SkillArguments.Problems(Parameters, Json(arguments)), Is.Empty);

    /// <summary>Each fault is one problem, naming the parameter and what is wrong.</summary>
    [TestCase("""{ "limit": 3 }""", "'text'")]
    [TestCase("""{ "text": "a", "colour": "red" }""", "'colour'")]
    [TestCase("""{ "text": 7 }""", "string")]
    [TestCase("""{ "text": "a", "limit": 2.5 }""", "integer")]
    [TestCase("""{ "text": "a", "loud": "yes" }""", "boolean")]
    [TestCase("""["a"]""", "object")]
    public void AFaultyCall_HasItsProblem(string arguments, string named)
    {
        var problems = SkillArguments.Problems(Parameters, Json(arguments));

        Assert.That(problems, Has.Count.EqualTo(1).And.Some.Contains(named));
    }

    /// <summary>Every fault of a call is named at once, not only the first.</summary>
    [Test]
    public void EveryProblem_IsNamed()
    {
        var problems = SkillArguments.Problems(Parameters, Json("""{ "colour": "red", "loud": 1 }"""));

        Assert.That(problems, Has.Count.EqualTo(3));
    }

    /// <summary>The call's arguments as the model sends them.</summary>
    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement;
}
