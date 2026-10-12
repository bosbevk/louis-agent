namespace LouisAgent.Cli.Messages;

using System.ComponentModel;

/// <summary>The CLI's screen text: the banner, the prompts, a reply's status lines.</summary>
public enum CliUserText
{
    [Description("=== Louis Agent CLI ===")]
    Banner,

    [Description("Type 'exit' to quit; Ctrl+C stops a reply; /usage shows this session's tokens and cost;")]
    BannerHelp,

    [Description("/tools, /approve <Name>, /reject <Name> manage agent-built tools\n")]
    BannerCommands,

    [Description("You: ")]
    YouPrompt,

    [Description("\nAssistant: ")]
    AssistantPrompt,

    [Description("\n[stopped]\n")]
    Stopped,

    [Description("\nError: {Reason}\n")]
    TurnError,

    [Description("Error: {Reason}")]
    PromptError,

    [Description("  > {Name} {Arguments}")]
    ToolCallStatus,

    [Description("    {Outcome}: {Result}")]
    ToolResultStatus,

    [Description("done")]
    ToolDone,

    [Description("failed")]
    ToolFailed,
}
