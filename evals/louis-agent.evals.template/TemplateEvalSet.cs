using LouisAgent.Choices;
using LouisAgent.Evals.Template.Evals.Example;

namespace LouisAgent.Evals.Template;

/// <summary>
/// This set's registration (docs/guides/add-an-eval-set.md, section 5): its one class at the project's root, the one the runner would ask for. This one
/// is never registered with the runner (<c>EvalSetKind</c> doesn't list it); a copy is registered in section 6.
/// </summary>
public sealed class TemplateEvalSet : IEvalSet
{
    // The identity the evals prove, in lower case; the name `EVALS=` picks the set by (section 6).
    public string Identity => TemplateIdentity.Template.Wire();

    // Each eval of the set, one folder each under Evals/ (section 1).
    public IReadOnlyList<IEval> Evals { get; } = [new ExampleEval()];
}

/// <summary>
/// The identity this set proves (ADR 0043: each eval set keeps its own enum of the names it uses): your identity's name. A copy renames the type and its member and gives the name on the wire,
/// <c>[WireName("acme")] Acme</c>. The set owns it: an eval belongs to exactly one set, so the catalogue lists each eval under its set's identity and no eval says it itself.
/// </summary>
public enum TemplateIdentity
{
    [WireName("template")] Template,
}
