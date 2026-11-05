namespace LouisAgent.AcpServer.Messages;

using System.ComponentModel;

/// <summary>The ACP server's log lines ([ACP] …); written to stderr, never stdout, which carries the protocol.</summary>
public enum AcpLogMessage
{
    [Description("Resource link path extracted: {Path}")]
    ResourceLinkPath,

    [Description("Searching for git root from: {Path}")]
    SearchingGitRoot,

    [Description("FindGitRoot returned: {Root}")]
    FindGitRootReturned,

    [Description("✓ Detected git root from resource_link: {Root}")]
    GitRootDetected,

    [Description("✓ Set AGENT_WORKING_DIRECTORY to: {Root}")]
    WorkingDirectorySet,

    [Description("✗ No git root found for: {Path}")]
    NoGitRoot,

    [Description("✗ No resource_link found in parameters")]
    NoResourceLink,

    [Description("Raw parameters:\n{Parameters}\n")]
    RawParameters,

    [Description("Prompt: {Chars} chars: {Prompt}")]
    PromptReceived,

    [Description("Usage: {Line}")]
    UsageLine,

    [Description("Agent operation cancelled: {Reason}")]
    OperationCancelled,

    [Description("Inner exception: {ErrorType}: {Reason}")]
    InnerException,

    [Description("Stack trace: {Trace}")]
    StackTrace,

    [Description("Unexpected error in PromptAsync: {ErrorType}: {Reason}")]
    PromptFailed,

    [Description("Embedded resource {Uri}: {Chars} chars")]
    EmbeddedResource,

    [Description("Attached file not found in the container: {Uri}")]
    AttachedFileMissing,

    [Description("Attached file {Name} read from {Path}: {Chars} chars")]
    AttachedFileRead,

    [Description("Error reading attached file {Path}: {Reason}")]
    AttachedFileReadFailed,

    [Description("FindGitRoot: normalized path = {Path}")]
    FindGitRootNormalized,

    [Description("Trying direct path...")]
    TryingDirectPath,

    [Description("  ✓ Found .git (direct path): {Path}")]
    FoundGitDirect,

    [Description("  - No .git at: {Path}")]
    NoGitAt,

    [Description("Direct path search exhausted. Generating container path candidates...")]
    DirectPathExhausted,

    [Description("Generated {Count} container path candidates")]
    CandidatesGenerated,

    [Description("Trying container path: {Path}")]
    TryingContainerPath,

    [Description("  Depth {Depth}: checking {Path}")]
    CheckingDepth,

    [Description("  ✓ Found .git (mapped path): {Path}")]
    FoundGitMapped,

    [Description("✗ No .git found in any candidate paths")]
    NoGitFound,

    [Description("Added candidate from ACP_MOUNT_MAPPINGS: {Path}")]
    CandidateFromMappings,

    [Description("Found matching file in {Root}: {File}")]
    MatchingFile,

    [Description("Request failed")]
    RequestFailed,

    [Description("The ACP server stopped")]
    ServerStopped,
}
