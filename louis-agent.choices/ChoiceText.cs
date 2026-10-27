using System.ComponentModel;
using System.Reflection;

namespace LouisAgent.Choices;

/// <summary>Fills a <see cref="ChoiceMessage"/>'s named holes (<c>{Enum}</c>, <c>{Value}</c>): the one-purpose stand-in for core's <c>MessageText</c>, which this project cannot reach.</summary>
internal static class ChoiceText
{
    public static string Format(ChoiceMessage message, string enumName, object value) =>
        typeof(ChoiceMessage).GetField(message.ToString())!.GetCustomAttribute<DescriptionAttribute>()!.Description
            .Replace("{Enum}", enumName, StringComparison.Ordinal)
            .Replace("{Value}", Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
}
