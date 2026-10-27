using LouisAgent.Choices;

namespace LouisAgent.Evals.Template.Evals.Example;

/// <summary>
/// The evals of this set, by the names they are listed, run and stored under (ADR 0043: a name that repeats is an enum, and each eval set keeps its own; docs/guides/add-an-eval-set.md,
/// "Your names are an enum"). A copy renames the type and its members, and gives each name on the wire: <c>[WireName("acme-quotes")] Quotes</c>. Its modes and roles get enums the same way.
/// </summary>
public enum TemplateEvalName
{
    [WireName("template-example")] Example,
}
