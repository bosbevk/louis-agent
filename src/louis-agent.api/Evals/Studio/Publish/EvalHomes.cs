using LouisAgent.Api.Evals.Drafts.Composed;
using LouisAgent.Api.Evals.List;

namespace LouisAgent.Api.Evals.Studio.Publish;

/// <summary>
/// Where an eval being composed lives in the workspace (FX32): an eval the runner already lists is in the folder its
/// listing names (the cook's <c>Cook</c>), so an edit starts from that folder and is published back into it; a new
/// eval has none yet and goes where <see cref="PublishTarget"/> puts a new one.
/// </summary>
/// <remarks>
/// Information Expert (GRASP): the runner's listing is the one that knows each eval's folder (finding R), as the
/// sample drafts already ask it (<c>ScenarioFiles</c>), so the studio asks the catalogue, never a path the person
/// typed. The folder is found behind <see cref="EvalPaths"/>, the guard the manuals use.
/// </remarks>
public sealed record EvalHomes(EvalList Catalog, string WorkspaceRoot)
{
    /// <summary>The eval's folder here, when the runner lists it and it's in this workspace; none if new.</summary>
    /// <remarks>
    /// Guard clauses: not listed, listed outside its identity's <c>Evals/</c>, outside the workspace, not here.
    /// </remarks>
    public string? Existing(EvalDraft draft)
    {
        if (ListedFolder(draft) is not { } listed) return null;
        string folder = TargetOf(draft).Folder;
        if (folder != listed.Replace('\\', '/').TrimEnd('/')) return null;
        if (EvalPaths.Inside(WorkspaceRoot, folder) is not { } path) return null;
        return Directory.Exists(path) ? path : null;
    }

    /// <summary>Where the eval is published: its listed folder for an edit, a new one for a new eval.</summary>
    public PublishTarget TargetOf(EvalDraft draft) => PublishTarget.Of(draft.Identity, draft.Name, ListedFolder(draft));

    /// <summary>The folder the runner lists the eval of this name in, when it lists one for this identity.</summary>
    private string? ListedFolder(EvalDraft draft) =>
        Catalog.Checks.Select(entry => entry.Eval).FirstOrDefault(eval =>
            eval is not null && eval.Name == draft.Name && (eval.Identity ?? draft.Identity) == draft.Identity)?.Folder;
}
