using System.Text.Json;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Reviews;
using LouisAgent.Puls.Admin.Reviews;

namespace LouisAgent.Puls.Tests.Admin.Reviews;

/// <summary>
/// The API's judge checks (the contract's <c>CalibrationsReply</c>) into what the card shows (F18 step 6a.5c). Each
/// class comes in the agreement card's shape, so it is the same <c>ClassAgreement</c> the agreement sentences read;
/// the list is as the API orders it, newest first.
/// </summary>
public class CalibrationViewTests
{
    /// <summary>Two checks of one judge, the newest first, the older with a disagreement.</summary>
    private static readonly CalibrationsReply Two = JsonSerializer.Deserialize<CalibrationsReply>(SampleMode.Fill(
        """
        {"calibrations":[
          {"run":"76667d39dcdb","grader":"meal-for-people","judge_model":"qwen3:4b","at":"2026-10-26T14:02:00+00:00",
           "agreed":36,"total":36,"bar":90,"passes":true,
           "person_pass":{"judge_pass":27,"judge_fail":0,"judge_unknown":0},
           "person_fail":{"judge_pass":0,"judge_fail":9,"judge_unknown":0},"disagreements":[]},
          {"run":"76667d39dcdb","grader":"meal-for-people","judge_model":null,"at":"2026-10-26T13:00:00+00:00",
           "agreed":35,"total":36,"bar":90,"passes":false,
           "person_pass":{"judge_pass":27,"judge_fail":0,"judge_unknown":0},
           "person_fail":{"judge_pass":1,"judge_fail":8,"judge_unknown":0},
           "disagreements":[{"scenario":"cook-shops","mode":"<ModeBeta>","repeat":2,"label":"fail","judge":"pass",
                             "reason":"a dish with steps"}]}
        ]}
        """), AdminJson.SnakeCase)!;

    /// <summary>The reports come as the API orders them, each class as agreement.</summary>
    [Test]
    public void TheReports_ComeAsTheAPIOrdersThem_WithTheirClassesAsAgreement()
    {
        var reports = CalibrationView.Of(Two);

        Assert.That(reports.Select(r => r.Agreed), Is.EqualTo(new[] { 36, 35 }));
        var latest = reports[0];
        Assert.That((latest.Grader, latest.JudgeModel, latest.Total, latest.Bar, latest.Passes),
            Is.EqualTo(("meal-for-people", "qwen3:4b", 36, 90, true)));
        Assert.That(latest.At, Is.EqualTo(new DateTimeOffset(2026, 10, 26, 14, 2, 0, TimeSpan.Zero)));
        Assert.That(latest.PersonPass, Is.EqualTo(new ClassAgreement(27, 0, 0, PersonPassed: true)));
        Assert.That(latest.PersonFail, Is.EqualTo(new ClassAgreement(0, 9, 0, PersonPassed: false)));
        Assert.That(latest.Disagreements, Is.Empty);
        var older = reports[1];
        Assert.That(older.JudgeModel, Is.Null);
        Assert.That(older.Disagreements,
            Is.EqualTo(new[]
            {
                new CalibrationDisagreementView("cook-shops", SampleMode.Beta, 2, "fail", "pass",
                    "a dish with steps")
            }));
    }

    /// <summary>No reports is an empty list, not an error.</summary>
    [Test]
    public void NoReports_IsAnEmptyList_NotAnError() =>
        Assert.That(CalibrationView.Of(new CalibrationsReply([])), Is.Empty);
}
