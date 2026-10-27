using LouisAgent.Api.Contracts.Info;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Skills.Roles;

namespace LouisAgent.Api.Endpoints.Info;

/// <summary>
/// Builds the instance's own replies from what it was built with: its model, its roles, and its toolsets.
/// </summary>
/// <remarks>
/// Replace Constructor with Factory Function (Fowler), as C# 14 static extension members, for the reason the admin
/// replies give (the unattended run's finding 1): the records live in the contract, which can't see the engine
/// factory, a toolset or the LLM settings, so each <c>Of</c> lives here and still reads as <c>Record.Of</c>.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
internal static class InfoReplyFactories
{
    /// <summary>The model reply names the provider and the model from the LLM settings.</summary>
    extension(ModelReply)
    {
        /// <summary>The model this instance answers with.</summary>
        public static ModelReply Of(LlmOptions llm) => new(llm.Provider, llm.Model);
    }

    /// <summary>The roles reply lists each level's enabled roles beside the default voice.</summary>
    extension(RolesReply)
    {
        /// <summary>The roles a session here can choose, per level.</summary>
        public static RolesReply Of(AgentEngineFactory engines) => new(
            engines.DefaultVoice,
            engines.AvailableRoles(RoleLevel.Agent),
            engines.AvailableRoles(RoleLevel.Specialist),
            engines.AvailableRoles(RoleLevel.Minion));
    }

    /// <summary>A toolset's row says whether it can load here, from the settings it lacks.</summary>
    extension(ToolsetRow)
    {
        /// <summary>One toolset as a session sees it before it chooses.</summary>
        public static ToolsetRow Of(ToolsetDefinition toolset, ToolsetContext context) => new(
            toolset.Name,
            toolset.Description,
            toolset.IsAvailable(context),
            toolset.MissingSettings(context));
    }
}
