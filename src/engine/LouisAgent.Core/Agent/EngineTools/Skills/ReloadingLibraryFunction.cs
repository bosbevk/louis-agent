using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Agent.EngineTools.Skills;

/// <summary>
/// The reload tool, which also reads the skills folder again (F97.1), so a skill dropped in by hand or changed on disk
/// is reachable at once through the door and listed in the next chat.
/// </summary>
/// <remarks>
/// A Decorator on Microsoft.Extensions.AI's <see cref="DelegatingAIFunction"/>, as <c>ListedSkillsFunction</c> is: the
/// skill files' own reload is the inner function's, untouched until step 14 replaces it, and the library's reload runs
/// first, whether or not the host gave the skill files a reloader.
/// </remarks>
internal sealed class ReloadingLibraryFunction(AIFunction reload, Action reloadLibrary) : DelegatingAIFunction(reload)
{
    /// <summary>Reads the skills folder again, then reloads the skill files as before.</summary>
    protected override ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments,
        CancellationToken cancellationToken)
    {
        reloadLibrary();
        return base.InvokeCoreAsync(arguments, cancellationToken);
    }
}
