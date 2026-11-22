using System.Reflection;
using System.Text.RegularExpressions;
using LouisAgent.Api.Messages;
using LouisAgent.Composition.Messages;
using LouisAgent.Testing.Messages;
using TemplateText = System.ComponentModel.DescriptionAttribute;

namespace LouisAgent.Api.Tests.Messages;

/// <summary>
/// The API's error texts are filled from named records, never anonymous objects (ADR 0119, CL70): every member whose
/// template has holes names its record, and every record matches its template.
/// </summary>
public partial class ApiErrorMessageHolesTests
{
    /// <summary>Every member with a hole in its template carries <c>[Holes]</c>.</summary>
    [Test]
    public void EveryMemberWithHoles_NamesItsRecord()
    {
        var missing = typeof(ApiErrorMessage).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.GetCustomAttribute<TemplateText>() is { } description &&
                            Hole().IsMatch(description.Description) &&
                            field.GetCustomAttribute<HolesAttribute>() is null)
            .Select(field => field.Name);

        Assert.That(missing, Is.Empty);
    }

    /// <summary>Every record a member names matches the holes of its template.</summary>
    [Test]
    public void EveryRecord_MatchesItsTemplate() =>
        Assert.That(MessageHoles.Problems([typeof(ApiErrorMessage)]), Is.Empty);

    /// <summary>A named hole in a template: matches <c>{Max}</c>, not <c>{</c> alone.</summary>
    [GeneratedRegex(@"\{\w+\}")]
    private static partial Regex Hole();
}
