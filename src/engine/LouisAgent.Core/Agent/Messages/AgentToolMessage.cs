using LouisAgent.Composition.Messages;
using LouisAgent.Core.Agent.Messages.Holes;
using LouisAgent.Core.Agent.Messages.Holes.AgentBuilt;
using LouisAgent.Core.Agent.Messages.Holes.Calls;
using LouisAgent.Core.Agent.Messages.Holes.Commands;
using LouisAgent.Core.Agent.Messages.Holes.Frozen;
using LouisAgent.Core.Agent.Messages.Holes.Oversized;
using LouisAgent.Core.Agent.Messages.Holes.SkillFiles;
using LouisAgent.Core.Agent.Messages.Holes.Skills;
using LouisAgent.Core.Agent.Messages.Holes.ToolBuilding;
using System.ComponentModel;

namespace LouisAgent.Core.Agent.Messages;

/// <summary>
/// Strings for the agent's own tools, skills and turn, returned to the model: what went wrong and what to do instead.
/// Each member is named for what happened, and its [Description] is the text, with named holes filled by
/// MessageText.Format from the record its [Holes] names (src/docs/CLAUDE.md, *Messages and logging*).
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0119-declare-name-then-pass.md">ADR 0119: named types, declared, then passed</seealso>
public enum AgentToolMessage
{
    [Description(
        "No tools are available in this session because the configured model does not support tool calling. If the " +
        "user asks you to perform an action (log time, run a skill, read or write files), say plainly that the " +
        "current model cannot do that and suggest switching LLM_MODEL to a tool-capable model. Never pretend to have " +
        "performed an action.")]
    NoToolsNotice,

    [Description(
        "[automatic notice] Your previous reply was cut off by the output limit, so any tool call you were writing " +
        "was lost and NOT run. Continue the task from where it stopped, writing large content in smaller chunks " +
        "(AppendToFile, at most ~300 lines per call).")]
    ContinueAfterCutOff,

    [Description(
        "[automatic notice] End your answer by calling {Tool} with the person's next choices, another page among " +
        "them when this one is done. Write no more words.")]
    [Holes(typeof(EndWithToolHoles))]
    EndWithTool,

    [Description(
        "Error: this tool call was cut off by the output limit before its arguments were complete, so it was NOT " +
        "run. Retrying the same call will fail the same way. Split large content into chunks of at most ~300 lines: " +
        "WriteWorkspaceFile with the first chunk, then AppendToFile for each following chunk, one call at a time, " +
        "until done.")]
    TruncatedCall,

    [Description("Error: {Tool} was NOT run: it needs every argument below. Call it again with them:\n{Missing}")]
    [Holes(typeof(ArgumentsMissingHoles))]
    ArgumentsMissing,

    [Description("- {Name}: {Meaning}")]
    [Holes(typeof(ArgumentMissingHoles))]
    ArgumentMissing,

    [Description(
        "[{Tool} returned {Chars} chars, too large to include verbatim. If exact content is needed, call it again " +
        "with narrower arguments (e.g. a specific file path).]")]
    [Holes(typeof(OversizedResultNoteHoles))]
    OversizedResultNote,

    [Description("\n... [{Chars} more chars not shown]")]
    [Holes(typeof(SummaryInputCutHoles))]
    SummaryInputCut,

    [Description(
        "You condense tool output for another AI agent that cannot see the original. Keep what matters for the " +
        "user's request: file names, counts, errors, key values and the substance of changes. Be concise and " +
        "factual. If the output looks mechanical (e.g. every line changed only by line endings or whitespace), say " +
        "so plainly.")]
    SummarySystemPrompt,

    [Description("User's request: {Request}\nTool: {Tool}({Arguments})\n\nOutput:\n{Output}")]
    [Holes(typeof(SummaryRequestHoles))]
    SummaryRequest,

    [Description("{Note}\nSummary:\n{Summary}")]
    [Holes(typeof(SummarisedResultHoles))]
    SummarisedResult,

    [Description("Error: tools are not supported by the current model.")]
    ToolsNotSupported,

    [Description("Error: tool building is not available (no skills folder was found).")]
    ToolBuildingUnavailable,

    [Description("Error: invalid tool definition:\n- {Errors}")]
    [Holes(typeof(ToolDefinitionInvalidHoles))]
    ToolDefinitionInvalid,

    [Description("Error: an approved tool named '{Name}' already exists; choose another name.")]
    [Holes(typeof(ApprovedToolExistsHoles))]
    ApprovedToolExists,

    [Description("Error: a pending tool named '{Name}' already exists. Pass overwrite=true to replace it.")]
    [Holes(typeof(PendingToolExistsHoles))]
    PendingToolExists,

    [Description("Error: '{Name}' is the name of a built-in tool; choose another name.")]
    [Holes(typeof(BuiltInToolNameHoles))]
    BuiltInToolName,

    [Description("[Timeout] {Name} exceeded {Seconds}s and was killed.\n{Output}")]
    [Holes(typeof(AgentToolTimeoutHoles))]
    AgentToolTimeout,

    [Description("Error: Skill name cannot be empty.")]
    SkillNameEmpty,

    [Description("Error: Skill '{Name}' was not found in the loaded skill definitions. Available skills: {Available}")]
    [Holes(typeof(SkillNotFoundHoles))]
    SkillNotFound,

    [Description("Error: Invalid JSON arguments: expected an object.")]
    SkillArgumentsNotObject,

    [Description("Error: Invalid JSON arguments: {Reason}")]
    [Holes(typeof(SkillArgumentsInvalidHoles))]
    SkillArgumentsInvalid,

    [Description("Error: Skill '{Name}' template is empty after substitution.")]
    [Holes(typeof(SkillTemplateEmptyHoles))]
    SkillTemplateEmpty,

    [Description("Failed to execute skill '{Name}': {Reason}")]
    [Holes(typeof(SkillFailedHoles))]
    SkillFailed,

    [Description("Error: skill authoring is not available (no skills folder was found).")]
    SkillAuthoringUnavailable,

    [Description("Error: name must be kebab-case letters/digits, e.g. 'csv-reports'.")]
    SkillFileNameInvalid,

    [Description("Error: markdown content is empty.")]
    SkillMarkdownEmpty,

    [Description("Error: '{File}' is a built-in skills file and cannot be replaced; choose another name.")]
    [Holes(typeof(SkillFileBuiltInHoles))]
    SkillFileBuiltIn,

    [Description("Error: '{File}' already exists. Pass overwrite=true to replace it, or pick another name.")]
    [Holes(typeof(SkillFileExistsHoles))]
    SkillFileExists,

    [Description(
        "Error: {Declared} '## Skill:' section(s) found but only {Valid} are valid. Each needs a '- Description:' " +
        "line and an '- Execution:' line followed by a ```bash code block.")]
    [Holes(typeof(SkillSectionsInvalidHoles))]
    SkillSectionsInvalid,

    [Description("Error: skill reloading is not available in this host.")]
    SkillReloadUnavailable,

    [Description("[Timeout] Command execution exceeded {Milliseconds}ms limit.")]
    [Holes(typeof(CommandTimeoutHoles))]
    CommandTimeout,

    [Description("[Exit Code {Code}] Error: {Output}")]
    [Holes(typeof(ExitCodeErrorHoles))]
    ExitCodeError,

    [Description("no parameters")] NoParameters,

    [Description("- {Name} [pending from an earlier session, not loaded]")]
    [Holes(typeof(AgentToolNotLoadedHoles))]
    AgentToolNotLoaded,

    [Description("No agent-built tools yet.")]
    NoAgentTools,

    [Description("Tool ran successfully (no output).")]
    ToolRanNoOutput,

    [Description("guidance only (no executable skills)")]
    SkillGuidanceOnly,

    [Description(
        "Saved {Path}, pending — {Skills}. ExecuteSkill runs it here now, and its documentation reaches the next " +
        "chat's system prompt; ask the user to type /approve {Name} to keep it for every later session, or " +
        "/reject {Name} to discard it.\n{Reload}")]
    [Holes(typeof(SkillFileSavedHoles))]
    SkillFileSaved,

    [Description(
        "Skills reloaded. Available skills: {Skills}. ExecuteSkill runs them here now; the next chat's system prompt " +
        "lists them.")]
    [Holes(typeof(SkillsReloadedHoles))]
    SkillsReloaded,

    [Description("Command executed successfully (no output returned).")]
    CommandRanNoOutput,

    [Description(
        "Created tool '{Name}' ({Language}; {Parameters}). It reaches the user's next chat, not this one: this chat " +
        "keeps the tools it started with. It is pending: tell the user to type '/approve {Name}' to keep it for " +
        "future sessions (or '/reject {Name}').")]
    [Holes(typeof(ToolCreatedHoles))]
    ToolCreated,

    [Description(
        "Error: '{Name}' was NOT run: the user rejected it, or it is no longer offered. Don't call it again in this " +
        "chat.")]
    [Holes(typeof(ToolNoLongerOfferedHoles))]
    ToolNoLongerOffered,

    [Description("- {Name} [approved]: {Description}")]
    [Holes(typeof(AgentToolApprovedHoles))]
    AgentToolApproved,

    [Description("- {Name} [pending, in chats started since it was made]: {Description}")]
    [Holes(typeof(AgentToolPendingHoles))]
    AgentToolPending,
}
