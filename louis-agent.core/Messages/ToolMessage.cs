namespace LouisAgent.Core.Messages;

using System.ComponentModel;

// The message enums (docs/CLAUDE.md, *Messages and logging*). Each member is named for what happened, and its
// [Description] is the text, with named holes filled by MessageText.Format (or by the structured logger). Members are added
// as code is written or moved; the refactor after F4 moves the existing messages here.

/// <summary>Strings tools return to the model: what went wrong and what to do instead.</summary>
public enum ToolMessage
{
    [Description("refusing to fetch {Host}: local addresses are not allowed.")]
    FetchLocalAddress,

    [Description("could not resolve {Host}")]
    FetchUnresolvable,

    [Description("refusing to fetch {Host}: it resolves to a private or local address ({Address}).")]
    FetchPrivateAddress,

    [Description("refusing to connect to {Host}: private or local address.")]
    ConnectPrivateAddress,

    [Description("Workspace paths must be relative.")]
    PathNotRelative,

    [Description("Path must stay inside the workspace.")]
    PathOutsideWorkspace,

    [Description("Workspace paths cannot traverse symbolic links.")]
    PathThroughSymbolicLink,

    [Description("Error: specify the project to run (see DotNetListProjects).")]
    DotNetProjectRequired,

    [Description("Error: filter must be '', 'outdated' or 'vulnerable'.")]
    DotNetPackageFilterInvalid,

    [Description("Error: '{Path}' is outside the workspace.")]
    PathOutsideWorkspaceNamed,

    [Description("Error: '{Project}' not found. Use DotNetListProjects to see available projects.")]
    DotNetProjectNotFound,

    [Description("[Exit Code {Code}] {Output}")]
    ExitCode,

    [Description("[Timeout] Git command exceeded 30s limit.")]
    GitTimeout,

    [Description("Error running git: {Reason}")]
    GitFailed,

    [Description("Invalid file path.")]
    InvalidFilePath,

    [Description("Error searching for files: {Reason}")]
    FileSearchFailed,

    [Description("Error: pass exactly one of 'script' or 'scriptPath'.")]
    PassScriptOrScriptPath,

    [Description("Error: pass exactly one of 'code' or 'scriptPath'.")]
    PassCodeOrScriptPath,

    [Description("Error: script '{Path}' not found.")]
    ScriptNotFound,

    [Description("Error: scriptPath must be a .ps1 file.")]
    ScriptPathNotPs1,

    [Description("Error: specify at least one package.")]
    PackagesRequired,

    [Description("Error: '{Specifier}' is not a valid package specifier.")]
    PackageSpecifierInvalid,

    [Description("Error: could not create the virtual environment at {Directory} (on Debian/Ubuntu install the 'python3-venv' package).\n{Output}")]
    VenvNotCreated,

    [Description("Bash is not available. On Windows install Git for Windows (https://git-scm.com, includes Git Bash) or set BASH_PATH to bash.exe. WSL's bash.exe in System32 is deliberately not used.")]
    BashNotInstalled,

    [Description("PowerShell is not available. Install PowerShell 7 (https://aka.ms/powershell or 'winget install Microsoft.PowerShell'), or set POWERSHELL_PATH to pwsh/powershell.exe.")]
    PowerShellNotInstalled,

    [Description("Python 3 is not installed or not on PATH. Install it (https://www.python.org/downloads/ or 'winget install Python.Python.3.12'), or set PYTHON_PATH to the interpreter. On Windows, the 'python' Microsoft Store alias does not count as an install.")]
    PythonNotInstalled,

    [Description("Error: unknown parameter '{Name}'. Parameters: {Known}.")]
    ScriptToolUnknownParameter,

    [Description("Error: missing required parameter '{Name}' ({Type}).")]
    ScriptToolMissingParameter,

    [Description("Error: parameter '{Name}' must be a {Type}, got {Value}.")]
    ScriptToolWrongType,

    [Description("Error: query cannot be empty.")]
    SearchQueryEmpty,

    [Description("Error: freshness must be day, week, month or year.")]
    SearchFreshnessInvalid,

    [Description("Error: {Reason}")]
    ErrorWithReason,

    [Description("Error: web search via {Provider} failed ({Reason}). Check the internet connection and try again.")]
    SearchFailed,

    [Description("Error: url must be an absolute http:// or https:// URL.")]
    FetchUrlInvalid,

    [Description("Error: {Url} returned HTTP {Status} {Reason}.")]
    FetchHttpError,

    [Description("Error: {Url} is '{MediaType}', which cannot be read as text.")]
    FetchNotText,

    [Description("Error: startIndex {StartIndex} is past the end of the content ({Length} characters).")]
    FetchStartPastEnd,

    [Description("Error: the response is larger than {Megabytes} MB.")]
    FetchTooLarge,

    [Description("Error: could not fetch {Url} ({Reason}).")]
    FetchFailed,

    [Description("more than {Count} redirects")]
    FetchTooManyRedirects,

    [Description("redirect to unsupported scheme '{Scheme}'.")]
    FetchRedirectScheme,

    [Description("DuckDuckGo refused the automated search. For reliable search set WEB_SEARCH_PROVIDER=google with GOOGLE_SEARCH_API_KEY and GOOGLE_SEARCH_ENGINE_ID.")]
    DuckDuckGoRefused,

    [Description("Unknown WEB_SEARCH_PROVIDER '{Provider}'. Supported: {Supported}.")]
    UnknownSearchProvider,

    [Description("Error: DEVOPS_API_KEY is not set.")]
    DevOpsKeyNotSet,

    [Description("Error {Status}: {Content}")]
    DevOpsHttpError,

    [Description("Error retrieving work item {Id}: {Reason}")]
    DevOpsWorkItemFailed,

    [Description("Error retrieving sprints: {Reason}")]
    DevOpsSprintsFailed,

    [Description("Error {Status} looking up the current sprint: {Content} Pass a sprint name (GetAvailableSprints lists them).")]
    DevOpsCurrentSprintHttpError,

    [Description("Error: the team has no current sprint. Pass a sprint name (GetAvailableSprints lists them).")]
    DevOpsNoCurrentSprint,

    [Description("Error looking up the current sprint: {Reason} Pass a sprint name (GetAvailableSprints lists them).")]
    DevOpsCurrentSprintFailed,

    [Description("Error executing WIQL query: {Reason}")]
    DevOpsQueryFailed,

    [Description("Error creating work item: {Reason}")]
    DevOpsCreateFailed,

    [Description("Error updating work item: {Reason}")]
    DevOpsUpdateFailed,

    [Description("ERROR: Paymo API key not configured.")]
    PaymoKeyNotConfigured,

    [Description("ERROR: Failed to {Action} (HTTP {Status})")]
    PaymoRequestHttpFailed,

    [Description("ERROR: Failed to {Action}: {Reason}")]
    PaymoRequestFailed,

    [Description("ERROR: {Item} not found (HTTP {Status})")]
    PaymoNotFound,

    [Description("ERROR: Paymo API key not configured. Set PAYMO_API_KEY environment variable.")]
    PaymoKeyNotConfiguredSetIt,

    [Description("ERROR: Task name or ID is required.")]
    PaymoTaskRequired,

    [Description("ERROR: Could not find task '{Task}'. Try using the numeric task ID instead.")]
    PaymoTaskNotFoundByName,

    [Description("ERROR: Duration must be greater than zero.")]
    PaymoDurationNotPositive,

    [Description("ERROR: Failed to create time entry (HTTP {Status}). Response: {Response}")]
    PaymoCreateEntryFailed,

    [Description("ERROR: {Reason}")]
    PaymoError,

    [Description("ERROR: Unexpected error: {Reason}")]
    PaymoUnexpectedError,

    [Description("Task name or ID cannot be empty.")]
    PaymoTaskEmpty,

    [Description("Failed to resolve task ID for '{Task}': {Reason}")]
    PaymoTaskResolveFailed,

    [Description("Must provide either start_time/end_time OR hours parameter.")]
    PaymoTimeOrHoursRequired,

    [Description("Invalid start_time format: '{Time}'. Use HH:MM (e.g., '08:00').")]
    PaymoStartTimeInvalid,

    [Description("Invalid end_time format: '{Time}'. Use HH:MM (e.g., '09:00').")]
    PaymoEndTimeInvalid,

    [Description("Invalid hours value: '{Hours}'. Use a positive number (e.g., '2' or '1.5').")]
    PaymoHoursInvalid,

    [Description("Error running {Program}: {Reason}")]
    ProcessStartFailed,

    [Description("No tools are available in this session because the configured model does not support tool calling. If the user asks you to perform an action (log time, run a skill, read or write files), say plainly that the current model cannot do that and suggest switching LLM_MODEL to a tool-capable model. Never pretend to have performed an action.")]
    NoToolsNotice,

    [Description("[automatic notice] Your previous reply was cut off by the output limit, so any tool call you were writing was lost and NOT run. Continue the task from where it stopped, writing large content in smaller chunks (AppendToFile, at most ~300 lines per call).")]
    ContinueAfterCutOff,

    [Description("Error: this tool call was cut off by the output limit before its arguments were complete, so it was NOT run. Retrying the same call will fail the same way. Split large content into chunks of at most ~300 lines: WriteWorkspaceFile with the first chunk, then AppendToFile for each following chunk, one call at a time, until done.")]
    TruncatedCall,

    [Description("[{Tool} returned {Chars} chars, too large to include verbatim. If exact content is needed, call it again with narrower arguments (e.g. a specific file path).]")]
    OversizedResultNote,

    [Description("\n... [{Chars} more chars not shown]")]
    SummaryInputCut,

    [Description("You condense tool output for another AI agent that cannot see the original. Keep what matters for the user's request: file names, counts, errors, key values and the substance of changes. Be concise and factual. If the output looks mechanical (e.g. every line changed only by line endings or whitespace), say so plainly.")]
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

    [Description("Error: {Declared} '## Skill:' section(s) found but only {Valid} are valid. Each needs a '- Description:' line and an '- Execution:' line followed by a ```bash code block.")]
    SkillSectionsInvalid,

    [Description("Error: skill reloading is not available in this host.")]
    SkillReloadUnavailable,

    [Description("[Timeout] Command execution exceeded {Milliseconds}ms limit.")]
    CommandTimeout,

    [Description("[Exit Code {Code}] Error: {Output}")]
    ExitCodeError,

    [Description("Directory not found: {Path}")]
    DirectoryNotFound,

    [Description("Directory is empty.")]
    DirectoryEmpty,

    [Description("Reading this sensitive file is not allowed.")]
    SensitiveRead,

    [Description("File not found: {Path}")]
    FileNotFound,

    [Description("File is larger than the 1 MB reading limit.")]
    FileTooLarge,

    [Description("Search query cannot be empty.")]
    WorkspaceSearchQueryEmpty,

    [Description("No matches found.")]
    NoMatches,

    [Description("Writing this sensitive file is not allowed.")]
    SensitiveWrite,

    [Description("Wrote {Path} ({Length} characters).")]
    FileWritten,

    [Description("Directory already exists: {Path}")]
    DirectoryExistsAlready,

    [Description("Created directory: {Path}")]
    DirectoryCreated,

    [Description("Deleting this sensitive file is not allowed.")]
    SensitiveDelete,

    [Description("Deleted file: {Path}")]
    FileDeleted,

    [Description("This directory contains a sensitive file and cannot be deleted.")]
    SensitiveDirectoryDelete,

    [Description("Deleted directory: {Path}")]
    DirectoryDeleted,

    [Description("Renaming this sensitive file is not allowed.")]
    SensitiveRename,

    [Description("Path not found: {Path}")]
    PathNotFound,

    [Description("Destination already exists: {Path}")]
    DestinationExists,

    [Description("Copying this sensitive file is not allowed.")]
    SensitiveCopy,

    [Description("Source file not found: {Path}")]
    SourceFileNotFound,

    [Description("Copied {Source} to {Destination}")]
    FileCopied,

    [Description("Writing to this sensitive file is not allowed.")]
    SensitiveAppend,

    [Description("Appended to {Path} ({Length} characters).")]
    FileAppended,

    [Description("No files matching pattern '{Pattern}' found.")]
    NoFilesMatching,

    [Description("File exists: {Path}")]
    FileExists,

    [Description("Directory exists: {Path}")]
    DirectoryExists,

    [Description("Path does not exist: {Path}")]
    PathDoesNotExist,

    [Description("No uncommitted changes.")]
    NoUncommittedChanges,

    [Description("Uncommitted changes found:\n{Status}")]
    UncommittedChanges,

    [Description("Success (no output).")]
    SucceededNoOutput,

    [Description("No .NET solutions or projects found.")]
    NoDotNetProjects,

    [Description("Timed out.\n{Output}")]
    TimedOutWithOutput,

    [Description("Succeeded.")]
    BuildSucceeded,

    [Description("FAILED (exit code {ExitCode}).")]
    BuildFailed,

    [Description("... {Count} more warning(s)")]
    MoreWarnings,

    [Description("Test run timed out.\n{Output}")]
    TestRunTimedOut,

    [Description("... more failures omitted")]
    MoreFailures,

    [Description("Tests passed.")]
    TestsPassed,

    [Description("Program was still running after {Seconds}s and was killed.")]
    ProgramKilled,

    [Description("Exit code: {ExitCode}")]
    ProcessExitCode,

    [Description("Script was still running after {Seconds}s and was killed.")]
    ScriptKilled,

    [Description("\nHint: the script has Windows (CRLF) line endings; convert it to LF.")]
    BashCrlfHint,

    [Description("pip install timed out.\n{Output}")]
    PipTimedOut,

    [Description("pip install FAILED (exit code {ExitCode}).\n{Output}")]
    PipFailed,

    [Description("created")]
    VenvCreated,

    [Description("not created yet; PythonInstallPackages creates it")]
    VenvNotCreatedYet,

    [Description("Google search quota exceeded ({Detail}). Try again later or raise the quota in Google Cloud.")]
    GoogleQuotaExceeded,

    [Description("Google rejected the request ({Detail}). Check GOOGLE_SEARCH_API_KEY, GOOGLE_SEARCH_ENGINE_ID and that the Custom Search API is enabled.")]
    GoogleRejected,

    [Description("Google search failed: HTTP {Status} {Detail}")]
    GoogleFailed,

    [Description("No results for {Query} (via {Provider}). Try broader or different keywords.")]
    NoSearchResults,

    [Description("Search results for {Query} (via {Provider}):\n")]
    SearchResults,

    [Description("(no readable text)")]
    NoReadableText,

    [Description("\n[Showing characters {Start}-{End} of {Length}. Call FetchUrl with startIndex={End} to continue.]")]
    PageContinues,

    [Description("No sprints found.")]
    NoSprints,

    [Description("Created {Type} #{Id} - {Title}")]
    WorkItemCreated,

    [Description("Created work item: {Response}")]
    WorkItemCreatedRaw,

    [Description("Updated work item #{Id}: {Field} = {Value}")]
    WorkItemUpdated,

    [Description("✓ Time entry {Id} deleted successfully")]
    PaymoEntryDeleted,

    [Description("✓ Time entry created successfully")]
    PaymoEntryCreated,

    [Description("Timeout must be a number of seconds between 1 and 600, got '{Value}'.")]
    ScriptTimeoutInvalid,

    [Description("Parameter '{Name}' has unsupported type '{Type}' (use {Supported}).")]
    ScriptParameterTypeUnsupported,

    [Description("Cannot parse parameter line: '{Line}'. Expected: - `name` (string|integer|number|boolean, required|optional): description")]
    ScriptParameterLineInvalid,

    [Description("Tool name '{Name}' must be PascalCase letters/digits (3-64 chars), e.g. 'CsvSummary'.")]
    ScriptToolNameInvalid,

    [Description("Missing '- Implementation:' line followed by a fenced code block.")]
    ScriptImplementationMissing,

    [Description("Parameter '{Name}' is declared more than once.")]
    ScriptParameterDuplicate,

    [Description("'- Implementation:' must be followed by a fenced code block (```python, ```powershell or ```bash).")]
    ScriptImplementationNotFenced,

    [Description("Unsupported implementation language '{Language}' (use python, powershell or bash).")]
    ScriptLanguageUnsupported,

    [Description("The implementation code block is not closed with ```.")]
    ScriptFenceNotClosed,

    [Description("The implementation code block is empty.")]
    ScriptCodeEmpty,

    [Description("no parameters")]
    NoParameters,

    [Description("- {Name} [pending from an earlier session, not loaded]")]
    AgentToolNotLoaded,

    [Description("No agent-built tools yet.")]
    NoAgentTools,

    [Description("Tool ran successfully (no output).")]
    ToolRanNoOutput,

    [Description("guidance only (no executable skills)")]
    SkillGuidanceOnly,

    [Description("Saved {Path} — {Skills}.\n{Reload}")]
    SkillFileSaved,

    [Description("Skills reloaded. Available skills: {Skills}")]
    SkillsReloaded,

    [Description("Command executed successfully (no output returned).")]
    CommandRanNoOutput,

    [Description("Renamed file from {From} to {To}")]
    FileRenamed,

    [Description("Renamed directory from {From} to {To}")]
    DirectoryRenamed,

    [Description("Build failed before tests ran.\n{Build}")]
    BuildFailedBeforeTests,

    [Description("{Description} (Agent-built tool.)")]
    ScriptToolDescription,

    [Description("{Description} (Agent-built tool, pending user approval.)")]
    ScriptToolDescriptionPending,

    [Description("Created tool '{Name}' ({Language}; {Parameters}). You can call it now. It is pending: tell the user to type '/approve {Name}' to keep it for future sessions (or '/reject {Name}').")]
    ToolCreated,

    [Description("- {Name} [approved]: {Description}")]
    AgentToolApproved,

    [Description("- {Name} [pending, active this session]: {Description}")]
    AgentToolPending,

    [Description("WEB_SEARCH_PROVIDER=google needs both GOOGLE_SEARCH_API_KEY and GOOGLE_SEARCH_ENGINE_ID in config/.env.secrets.")]
    GoogleNotConfigured,

    [Description("[truncated at 200 entries]")]
    ListTruncated,

    [Description("\n[results truncated at 500 files]")]
    FindTruncated,

    [Description("Directory tree for {Path} (depth: {Depth}):\n")]
    DirectoryTree,

    [Description("Error: no peer agent named '{Agent}'. Peers (AGENT_PEERS): {Peers}.")]
    UnknownPeer,

    [Description("Error: this turn has already started {Max} sub-sessions, the most one turn may. Finish with what you have, or ask the user.")]
    SubSessionLimit,

    [Description("Error: {Agent} refused the sub-session (HTTP {Status}): {Reason}")]
    SubSessionRefused,

    [Description("Error: the sub-session on {Agent} failed: {Reason}")]
    SubSessionFailed,

    [Description("Error: the sub-session on {Agent} didn't finish within {Minutes} minutes and was stopped. Try a smaller task.")]
    SubSessionTimedOut,

    [Description("{Answer}\n\n[Sub-session on {Agent} ({Toolsets}): {Usage}]")]
    SubSessionAnswer,

    [Description("{Requests} requests, {Tokens} tokens, {Cost}")]
    SubSessionUsage,

    [Description("usage not reported")]
    SubSessionNoUsage,

    [Description("cost unknown")]
    SubSessionCostUnknown,
}
