
using System.ComponentModel;

namespace LouisAgent.Core.Agent.Messages;
// The message enums (docs/CLAUDE.md, *Messages and logging*). Each member is named for what happened, and its
// [Description] is the text, with named holes filled by MessageText.Format (or by the structured logger). Members are added
// as code is written or moved; the refactor after F4 moves the existing messages here.

/// <summary>Strings for the agent's own tools, skills and turn, returned to the model: what went wrong and what to do instead.</summary>
public enum AgentToolMessage
{
    [Description(
        "No tools are available in this session because the configured model does not support tool calling. If the user asks you to perform an action (log time, run a skill, read or write files), say plainly that the current model cannot do that and suggest switching LLM_MODEL to a tool-capable model. Never pretend to have performed an action.")]
    NoToolsNotice,

    [Description(
        "[automatic notice] Your previous reply was cut off by the output limit, so any tool call you were writing was lost and NOT run. Continue the task from where it stopped, writing large content in smaller chunks (AppendToFile, at most ~300 lines per call).")]
    ContinueAfterCutOff,

    [Description(
        "Error: this tool call was cut off by the output limit before its arguments were complete, so it was NOT run. Retrying the same call will fail the same way. Split large content into chunks of at most ~300 lines: WriteWorkspaceFile with the first chunk, then AppendToFile for each following chunk, one call at a time, until done.")]
    TruncatedCall,

    [Description(
        "[{Tool} returned {Chars} chars, too large to include verbatim. If exact content is needed, call it again with narrower arguments (e.g. a specific file path).]")]
    OversizedResultNote,

    [Description("\n... [{Chars} more chars not shown]")]
    SummaryInputCut,

    [Description(
        "You condense tool output for another AI agent that cannot see the original. Keep what matters for the user's request: file names, counts, errors, key values and the substance of changes. Be concise and factual. If the output looks mechanical (e.g. every line changed only by line endings or whitespace), say so plainly.")]
    SummarySystemPrompt,

    [Description("User's request: {Request}\nTool: {Tool}({Arguments})\n\nOutput:\n{Output}")]
    SummaryRequest,

    [Description("{Note}\nSummary:\n{Summary}")]
    SummarisedResult,

    [Description("Error: tools are not supported by the current model.")]
    ToolsNotSupported,

    [Description("Error: tool building is not available (no skills folder was found).")]
    ToolBuildingUnavailable,

    [Description("Error: invalid tool definition:\n- {Errors}")]
    ToolDefinitionInvalid,

    [Description("Error: an approved tool named '{Name}' already exists; choose another name.")]
    ApprovedToolExists,

    [Description("Error: a pending tool named '{Name}' already exists. Pass overwrite=true to replace it.")]
    PendingToolExists,

    [Description("Error: '{Name}' is the name of a built-in tool; choose another name.")]
    BuiltInToolName,

    [Description("[Timeout] {Name} exceeded {Seconds}s and was killed.\n{Output}")]
    AgentToolTimeout,

    [Description("Error: Skill name cannot be empty.")]
    SkillNameEmpty,

    [Description("Error: Skill '{Name}' was not found in the loaded skill definitions. Available skills: {Available}")]
    SkillNotFound,

    [Description("Error: Invalid JSON arguments: expected an object.")]
    SkillArgumentsNotObject,

    [Description("Error: Invalid JSON arguments: {Reason}")]
    SkillArgumentsInvalid,

    [Description("Error: Skill '{Name}' template is empty after substitution.")]
    SkillTemplateEmpty,

    [Description("Failed to execute skill '{Name}': {Reason}")]
    SkillFailed,

    [Description("Error: skill authoring is not available (no skills folder was found).")]
    SkillAuthoringUnavailable,

    [Description("Error: name must be kebab-case letters/digits, e.g. 'csv-reports'.")]
    SkillFileNameInvalid,

    [Description("Error: markdown content is empty.")]
    SkillMarkdownEmpty,

    [Description("Error: '{File}' is a built-in skills file and cannot be replaced; choose another name.")]
    SkillFileBuiltIn,

    [Description("Error: '{File}' already exists. Pass overwrite=true to replace it, or pick another name.")]
    SkillFileExists,

    [Description(
        "Error: {Declared} '## Skill:' section(s) found but only {Valid} are valid. Each needs a '- Description:' line and an '- Execution:' line followed by a ```bash code block.")]
    SkillSectionsInvalid,

    [Description("Error: skill reloading is not available in this host.")]
    SkillReloadUnavailable,

    [Description("[Timeout] Command execution exceeded {Milliseconds}ms limit.")]
    CommandTimeout,

    [Description("[Exit Code {Code}] Error: {Output}")]
    ExitCodeError,

    [Description("no parameters")] NoParameters,

    [Description("- {Name} [pending from an earlier session, not loaded]")]
    AgentToolNotLoaded,

    [Description("No agent-built tools yet.")]
    NoAgentTools,

    [Description("Tool ran successfully (no output).")]
    ToolRanNoOutput,

    [Description("guidance only (no executable skills)")]
    SkillGuidanceOnly,

    [Description(
        "Saved {Path}, pending — {Skills}. It runs in this session now; ask the user to type /approve {Name} to keep it for every later session, or /reject {Name} to discard it.\n{Reload}")]
    SkillFileSaved,

    [Description("Skills reloaded. Available skills: {Skills}")]
    SkillsReloaded,

    [Description("Command executed successfully (no output returned).")]
    CommandRanNoOutput,

    [Description(
        "Created tool '{Name}' ({Language}; {Parameters}). You can call it now. It is pending: tell the user to type '/approve {Name}' to keep it for future sessions (or '/reject {Name}').")]
    ToolCreated,

    [Description("- {Name} [approved]: {Description}")]
    AgentToolApproved,

    [Description("- {Name} [pending, active this session]: {Description}")]
    AgentToolPending,
}
