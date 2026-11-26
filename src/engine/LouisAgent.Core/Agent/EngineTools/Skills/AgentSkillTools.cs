using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Agent.EngineTools.Skills;

/// <summary>
/// The tools for the skills an agent makes and keeps (F97.1), handed together to the registry with the self-extension
/// tools: the door that reads and runs them, the tool that makes one, and the reload of the skills folder.
/// </summary>
/// <param name="Door"><c>UseSkill</c>, listing the approved skills.</param>
/// <param name="Create"><c>CreateSkill</c>.</param>
/// <param name="ReloadLibrary">Reads the skills folder again, when the skills are reloaded.</param>
internal sealed record AgentSkillTools(AIFunction Door, AIFunction Create, Action ReloadLibrary);
