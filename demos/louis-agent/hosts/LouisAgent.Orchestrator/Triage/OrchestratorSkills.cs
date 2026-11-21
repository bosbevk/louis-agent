using LouisAgent.Composition.Messages;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Headers;
using LouisAgent.Core.Skills.Providers;
using LouisAgent.Core.Skills.Roles;
using LouisAgent.Orchestrator.Messages;

namespace LouisAgent.Orchestrator.Triage;

/// <summary>
/// Builds the triage agent's system prompt: the principles every agent reads first, the orchestrator's workflow, then
/// the service's own page and one runbook per API method from Skills/{service}/.
/// </summary>
/// <remarks>
/// Composite: each part is its own skill provider, added in the order the model should read them, so the shared parts
/// come from core's built-in prompts and only the per-service runbooks live on disk.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0020-principles-for-every-agent.md">ADR 0020: principles for every agent</seealso>
internal static class OrchestratorSkills
{
    /// <summary>The orchestrator's mode, in the coding identity's prompts: its workflow, the same for every service.</summary>
    internal const string Mode = "louis-agent/voice-orchestrator";

    /// <summary>The service's own page in its runbook folder; every other file there is one method's runbook.</summary>
    private const string ServicePage = "service.md";

    /// <summary>
    /// Loads the prompt for one service, and the methods it has runbooks for. A service without a runbook folder is a
    /// setup mistake, so it throws.
    /// </summary>
    public static (ISkillProvider Skills, IReadOnlySet<string> Methods) Load(string skillsDirectory, string service)
    {
        string serviceDirectory = Path.Combine(skillsDirectory, service);
        if (!Directory.Exists(serviceDirectory))
            throw new InvalidOperationException(MessageText.Format(OrchestratorErrorMessage.NoRunbooks,
                new
                {
                    Service = service,
                    Directory = serviceDirectory
                }));

        // The shared parts first, from core: the principles, then the orchestrator's workflow.
        var composite = new CompositeSkillProvider();
        composite.AddProvider(MarkdownSkillProvider.FromText(BodyOf(SkillFiles.Principles)));
        composite.AddProvider(MarkdownSkillProvider.FromText(BodyOf(RoleCatalog.FileName(Mode, RoleLevel.Agent))));

        string serviceFile = Path.Combine(serviceDirectory, ServicePage);
        if (File.Exists(serviceFile)) composite.AddProvider(new MarkdownSkillProvider(serviceFile));

        // One runbook per method, in a fixed order so the prompt (and its cache) is the same on every run.
        var methods = new SortedSet<string>(StringComparer.Ordinal);
        foreach (string file in Directory.EnumerateFiles(serviceDirectory, "*.md").Order(StringComparer.Ordinal))
        {
            if (Path.GetFileName(file) == ServicePage) continue;
            composite.AddProvider(new MarkdownSkillProvider(file));
            methods.Add(Path.GetFileNameWithoutExtension(file));
        }

        return (composite, methods);
    }

    /// <summary>A built-in prompt as the model reads it: without its header (CL27), which is for the loader.</summary>
    private static string BodyOf(string path) => PromptCatalog.Get(path)!.Body;
}
