using LouisAgent.Choices;

namespace LouisAgent.Evals.Datasets.Samples;

/// <summary>
/// Who says a sample's input message. Inspect also knows <c>system</c>, <c>assistant</c> and <c>tool</c>, but our
/// scenarios replay a user's questions only, so the user is the one role a file may name.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum MessageRole
{
    [WireName("user")] User,
}
