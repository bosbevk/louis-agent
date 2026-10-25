using LouisAgent.Evals.Template.Evals.Example;

namespace LouisAgent.Evals.Template;

/// <summary>
/// The template's set (docs/guides/add-an-eval-set.md, section 5): the one class at the project's root, and the one the runner would ask for. This one
/// is never registered with the runner (<c>EvalSetKind</c> doesn't list it); a copy is registered in section 6.
/// </summary>
public sealed class TemplateEvalSet : IEvalSet
{
    // The identity the evals prove, in lower case; the name `EVALS=` picks the set by (section 6).
    public string Identity => "template";

    // Each eval of the set, one folder each under Evals/ (section 1).
    public IReadOnlyList<IEval> Evals { get; } = [new ExampleEval()];
}
