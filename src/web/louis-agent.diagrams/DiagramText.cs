using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using LouisAgent.Diagrams.Messages;

namespace LouisAgent.Diagrams;

/// <summary>
/// Fills a message's named holes from the properties of the same names: the one-purpose stand-in for core's
/// <c>MessageText</c>, which this library can't reach, as <c>ChoiceText</c> is for the choices. It fills any
/// message enum (CL65.1): the reader's refusals, <see cref="DiagramMessage"/>, and the words what builds a diagram
/// writes, so there is one formatter and not a copy a project. It sits beside the folders, since a
/// <c>Messages/</c> folder holds only the message enums.
/// </summary>
public static class DiagramText
{
    /// <summary>The text of a message that has no holes, as it is written.</summary>
    public static string Format<TMessage>(TMessage message) where TMessage : struct, Enum =>
        typeof(TMessage).GetField(Enum.GetName(message)!)!.GetCustomAttribute<DescriptionAttribute>()!.Description;

    /// <summary>The message's text, each <c>{Hole}</c> replaced by the value of the property with that name.</summary>
    public static string Format<TMessage>(TMessage message, object values) where TMessage : struct, Enum
    {
        string text = Format(message);
        foreach (var property in values.GetType().GetProperties())
            text = text.Replace($"{{{property.Name}}}",
                Convert.ToString(property.GetValue(values), CultureInfo.InvariantCulture), StringComparison.Ordinal);
        return text;
    }
}
