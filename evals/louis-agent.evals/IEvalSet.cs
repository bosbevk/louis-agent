namespace LouisAgent.Evals;

/// <summary>
/// The evals one identity proves (F18 step 4, ADR 0027): one set per evals project, so a new identity's evals are one project and
/// one set, with no list of evals to edit in the runner. The runner's catalogue picks the sets with EVALS=, as the composition
/// picks identities with IDENTITIES.
/// </summary>
public interface IEvalSet
{
    /// <summary>The identity the evals prove ("acme").</summary>
    string Identity { get; }

    IReadOnlyList<IEval> Evals { get; }
}
