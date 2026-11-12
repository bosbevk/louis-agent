using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using LouisAgent.Diagrams.Messages;

namespace LouisAgent.Diagrams;

/// <summary>
/// Fills a <see cref="DiagramMessage"/>'s named holes from the properties of the same names: the one-purpose
/// stand-in for core's <c>MessageText</c>, which this library can't reach, as <c>ChoiceText</c> is for the choices.
/// It sits beside the folders, since a <c>Messages/</c> folder holds only the message enums.
/// </summary>
internal static class DiagramText
{
    /// <summary>The message's text, each <c>{Hole}</c> replaced by the value of the property with that name.</summary>
    public static string Format(DiagramMessage message, object values)
    {
        string text = typeof(DiagramMessage).GetField(Enum.GetName(message)!)!
            .GetCustomAttribute<DescriptionAttribute>()!.Description;
        foreach (var property in values.GetType().GetProperties())
            text = text.Replace($"{{{property.Name}}}",
                Convert.ToString(property.GetValue(values), CultureInfo.InvariantCulture), StringComparison.Ordinal);
        return text;
    }
}
