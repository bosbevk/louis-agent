using System.Text.Json;
using LouisAgent.Api.Contracts.Admin.Studio;
using LouisAgent.Web.Evals.Admin.Studio.Compose;
using LouisAgent.Web.Evals.Admin.Studio.Schema;

namespace LouisAgent.Web.Evals.Tests.Admin.Studio.Compose;

/// <summary>
/// The compose page's model (F49.5): a check's fields are drawn from its settings' schema (a whole number, a number,
/// yes or no, a list, a reader, text; required where the schema says), and the form becomes the eval's file in the
/// order the eval folders write it, a field left empty left out; an eval's own file reads back into the form.
/// </summary>
public class EvalCompositionTests
{
    /// <summary>
    /// <c>amount-under</c>'s schema as the runner lists it: a key and a reader required, a yes-or-no kept.
    /// </summary>
    private static readonly CheckTypeRow AmountUnder = new("amount-under", "fixed-rule", JsonDocument.Parse(
        """
        {"type":["object","null"],"properties":{"key":{"type":"string"},"reader":{"type":"string"},
         "required":{"type":"boolean","default":true},"limit":{"type":"integer"},"share":{"type":["number","null"]},
         "toolsets":{"type":"array","items":{"type":"string"}}},"required":["key","reader"]}
        """).RootElement.Clone());

    /// <summary>Each setting is a field of its kind, required where the schema lists it.</summary>
    [Test]
    public void ACheck_HasAFieldPerSetting_OfItsKind()
    {
        var fields = SchemaFields.Of(AmountUnder);

        Assert.That(fields.Select(f => (f.Name, f.Kind, f.Required)), Is.EqualTo(new[]
        {
            ("key", FieldKind.Text, true),
            ("reader", FieldKind.Reader, true),
            ("required", FieldKind.YesNo, false),
            ("limit", FieldKind.Whole, false),
            ("share", FieldKind.Number, false),
            ("toolsets", FieldKind.List, false)
        }));
    }

    /// <summary>The form becomes the eval's file, each value as its kind, an empty one left out.</summary>
    [Test]
    public void TheForm_BecomesTheFile()
    {
        var eval = new EvalComposition
        {
            Name = "weather-eval",
            Identity = "weather-agent",
            Version = 1,
            Proves = "The agent says what the weather does"
        };
        eval.Modes.Add(new ModeEntry
        {
            Name = "weather",
            Role = "weather/voice",
            Toolsets = $"{SampleToolset.Alpha}, {SampleToolset.Beta}"
        });
        var check = new CheckEntry { Type = "amount-under", Name = "within-budget" };
        check.Values["key"] = "budget";
        check.Values["reader"] = "rand-total";
        check.Values["limit"] = "250";
        check.Values["required"] = "false";
        check.Values["toolsets"] = "web, shops";
        check.Values["share"] = "";
        eval.Checks.Add(check);

        var file = JsonDocument.Parse(eval.ToJson(SchemaFields.ByType([AmountUnder]))).RootElement;

        Assert.That(string.Join(",", file.EnumerateObject().Select(p => p.Name)),
            Is.EqualTo("schema_version,name,identity,version,proves,modes,checks"));
        Assert.That(file.GetProperty("modes")[0].GetProperty("toolsets").EnumerateArray().Select(t => t.GetString()),
            Is.EqualTo(new[] { SampleToolset.Alpha, SampleToolset.Beta }));
        var parameters = file.GetProperty("checks")[0].GetProperty("parameters");
        Assert.That((parameters.GetProperty("limit").GetInt32(), parameters.GetProperty("required").GetBoolean(),
                parameters.GetProperty("toolsets").GetArrayLength(), parameters.TryGetProperty("share", out _)),
            Is.EqualTo((250, false, 2, false)));
        Assert.That(file.GetProperty("checks")[0].GetProperty("name").GetString(), Is.EqualTo("within-budget"));
    }

    /// <summary>An eval's own file reads back into the form, and out again the same.</summary>
    [Test]
    public void AnEvalsFile_ReadsBackIntoTheForm()
    {
        string json = $$$"""
                         {"schema_version":1,"name":"weather-eval","identity":"weather-agent","version":2,
                          "proves":"Weather","modes":[{"name":"weather","role":"weather/voice","toolsets":["{{{SampleToolset.Alpha}}}"]}],
                          "checks":[{"type":"amount-under","parameters":{"key":"budget","reader":"rand-total","limit":9}}]}
                         """;

        var eval = EvalComposition.FromJson(json);

        Assert.That((eval.Name, eval.Version, eval.Modes.Single().Toolsets, eval.Checks.Single().Values["limit"]),
            Is.EqualTo(("weather-eval", 2, SampleToolset.Alpha, "9")));
        var again = JsonDocument.Parse(eval.ToJson(SchemaFields.ByType([AmountUnder]))).RootElement;
        Assert.That(again.GetProperty("checks")[0].GetProperty("parameters").GetProperty("limit").GetInt32(),
            Is.EqualTo(9));
    }
}
