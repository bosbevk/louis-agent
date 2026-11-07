using System.Text.Json;
using LouisAgent.Api.Contracts.Admin.Drafts;
using LouisAgent.Web.Admin.Reviews;
using LouisAgent.Web.Admin.Studio;

namespace LouisAgent.Web.Tests.Admin.Studio;

/// <summary>
/// The sample dialog's form starts from the scenario the conversation ran (a new id beside it, its expected values as
/// JSON, its categories), and turns what the person typed into the request: the expected values must be a JSON
/// object, the categories a comma list, a blank ideal answer none at all.
/// </summary>
public class SampleFormTests
{
    /// <summary>The conversation the sample comes from.</summary>
    private static readonly ConversationRef CookAf = new("run1", SampleMode.Beta, 2, "cook-af");

    /// <summary>The scenario it ran: cook-af, for four under R250.</summary>
    private static readonly ScenarioRow Scenario = new("cook-af", "Stellenbosch", "Africa/Johannesburg",
        JsonDocument.Parse("""{"serves":[4,6],"budget":250}""").RootElement.Clone(), ["language:af", "budget"]);

    /// <summary>The form starts with a new id beside the scenario's, its expected values and its categories.</summary>
    [Test]
    public void TheForm_StartsFromTheScenario()
    {
        var form = SampleForm.From(Scenario);

        Assert.That((form.Id, form.Categories), Is.EqualTo(("cook-af-2", "language:af, budget")));
        Assert.That(JsonDocument.Parse(form.Expected).RootElement.GetProperty("budget").GetInt32(), Is.EqualTo(250));
    }

    /// <summary>What the person typed becomes the request, trimmed, the categories split.</summary>
    [Test]
    public void WhatWasTyped_BecomesTheRequest()
    {
        var form = SampleForm.From(Scenario);
        form.Id = " cook-af-six ";
        form.Expected = """{"serves":[6,6]}""";
        form.Categories = "language:af, , follow-up ";
        form.Target = "Bobotie vir ses.";

        var (request, error) = form.Request(CookAf, "Louis");

        Assert.That(error, Is.Null);
        Assert.That((request!.Id, request.Target, request.Author, request.Conversation!.Task),
            Is.EqualTo(("cook-af-six", "Bobotie vir ses.", "Louis", "cook-af")));
        Assert.That(request.Categories, Is.EqualTo(new[] { "language:af", "follow-up" }));
        Assert.That(request.Expected!.Value.GetProperty("serves")[0].GetInt32(), Is.EqualTo(6));
    }

    /// <summary>A blank ideal answer is none at all.</summary>
    [Test]
    public void ABlankTarget_IsNone() =>
        Assert.That(SampleForm.From(Scenario).Request(CookAf, "Louis").Request!.Target, Is.Null);

    /// <summary>Expected values that aren't a JSON object are refused before anything is sent, saying why.</summary>
    [TestCase("{ not json")]
    [TestCase("[6]")]
    [TestCase("")]
    public void ExpectedValuesThatAreNotAnObject_AreRefused(string expected)
    {
        var form = SampleForm.From(Scenario);
        form.Expected = expected;

        var (request, error) = form.Request(CookAf, "Louis");

        Assert.That(request, Is.Null);
        Assert.That(error, Does.Contain("JSON object"));
    }
}
