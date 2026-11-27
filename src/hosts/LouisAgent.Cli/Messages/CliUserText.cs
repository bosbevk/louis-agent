using System.ComponentModel;
using LouisAgent.Cli.Messages.Holes;
using LouisAgent.Composition.Messages;

namespace LouisAgent.Cli.Messages;

/// <summary>The CLI's screen text: the banner, the prompts, a reply's status lines.</summary>
public enum CliUserText
{
    [Description("=== Louis Agent CLI ===")]
    Banner,

    [Description("Type 'exit' to quit; Ctrl+C stops a reply; /usage shows this session's tokens and cost;")]
    BannerHelp,

    [Description("/skills, /approve <Name>, /reject <Name> manage the skills the agent makes\n")]
    BannerCommands,

    [Description("You: ")] YouPrompt,

    [Description("\nAssistant: ")] AssistantPrompt,

    [Description("\n[stopped]\n")] Stopped,

    [Description("\nError: {Reason}\n")]
    [Holes(typeof(TurnErrorHoles))]
    TurnError,

    [Description("Error: {Reason}")]
    [Holes(typeof(PromptErrorHoles))]
    PromptError,

    [Description("  > {Name} {Arguments}")]
    [Holes(typeof(ToolCallStatusHoles))]
    ToolCallStatus,

    [Description("    {Outcome}: {Result}")]
    [Holes(typeof(ToolResultStatusHoles))]
    ToolResultStatus,

    [Description("done")] ToolDone,

    [Description("failed")] ToolFailed,
}
