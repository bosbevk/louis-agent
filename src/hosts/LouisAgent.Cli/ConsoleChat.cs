using LouisAgent.Cli.Messages;
using LouisAgent.Cli.Messages.Holes;
using LouisAgent.Composition.Messages;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Agent.Turns;
using LouisAgent.Core.Usage.Records;
using LouisAgent.Core.Usage.Reports;
using Microsoft.Extensions.AI;

namespace LouisAgent.Cli;

/// <summary>
/// The CLI's chat on the console: the interactive loop and single-prompt mode. It maps the engine's stream to the
/// terminal (the answer on stdout, thinking and tool status in grey, the usage line on stderr); the engine holds no
/// console code (src/docs/CLAUDE.md, Project layout, rule 4: hosts stay thin).
/// </summary>
internal sealed class ConsoleChat(AgentEngine engine)
{
    /// <summary>The host name the usage ledger records the CLI's requests under.</summary>
    private const string Host = "cli";

    /// <summary>What the user types to leave the chat.</summary>
    private const string ExitWord = "exit";

    /// <summary>How long a tool's status line may be before it is cut, so one call can't fill the screen.</summary>
    private const int MaxStatusChars = 160;

    /// <summary>Run the interactive chat loop.</summary>
    public async Task RunAsync()
    {
        var conversation = engine.StartConversation();
        // One CLI run is one session in the usage ledger.
        string session = $"{Host}_{Guid.NewGuid():N}";
        int turns = 0;
        var sessionUsage = new UsageSession(engine.Display);

        // Ctrl+C stops the reply in progress; with no reply running it exits as usual.
        CancellationTokenSource? turn = null;
        Console.CancelKeyPress += (_, e) =>
        {
            if (Volatile.Read(ref turn) is not { } active) return;
            e.Cancel = true;
            active.Cancel();
        };

        Console.WriteLine(MessageText.Format(CliUserText.Banner));
        Console.WriteLine(MessageText.Format(CliUserText.BannerHelp));
        Console.WriteLine(MessageText.Format(CliUserText.BannerCommands));

        while (true)
        {
            Console.Write(MessageText.Format(CliUserText.YouPrompt));
            string? userInput = Console.ReadLine();

            // null = stdin closed (EOF); blank lines are ignored rather than ending the session.
            if (userInput is null || userInput.Trim().Equals(ExitWord, StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(userInput))
            {
                continue;
            }

            // Approval commands and /usage come from the user only; they never reach the model.
            var usedSoFar = engine.RecordsUsage ? sessionUsage.Records : null;
            if (engine.HandleUserCommand(userInput, usedSoFar) is { } commandReply)
            {
                Console.WriteLine($"\n{commandReply}\n");
                continue;
            }

            using var cancellation = new CancellationTokenSource();
            Volatile.Write(ref turn, cancellation);
            using var usage = UsageScope.Begin(host: Host, session: session, turn: ++turns);
            try
            {
                Console.Write(MessageText.Format(CliUserText.AssistantPrompt));
                await StreamToConsoleAsync(conversation, userInput, cancellation.Token);
                Console.WriteLine("\n");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine(MessageText.Format(CliUserText.Stopped));
            }
            catch (Exception ex)
            {
                var failure = new TurnErrorHoles(ex.Message);
                Console.WriteLine(MessageText.Format(CliUserText.TurnError, failure));
            }
            finally
            {
                Volatile.Write(ref turn, null);
                // After a stopped or failed reply too: what its requests used was still charged.
                WriteUsageLine(sessionUsage.EndTurn(usage.Records));
            }
        }
    }

    /// <summary>Run a single prompt and exit (non-interactive mode).</summary>
    public async Task RunSinglePromptAsync(string userInput)
    {
        using var usage = UsageScope.Begin(host: Host, session: $"{Host}_{Guid.NewGuid():N}", turn: 1);
        try
        {
            await StreamToConsoleAsync(engine.StartConversation(), userInput);
            Console.WriteLine();
            // One prompt is the whole session, so the line has no session total.
            if (usage.Records.Count > 0) WriteUsageLine(UsageText.Line(usage.Records, display: engine.Display));
        }
        catch (Exception ex)
        {
            var failure = new PromptErrorHoles(ex.Message);
            Console.WriteLine(MessageText.Format(CliUserText.PromptError, failure));
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// The usage line, in grey on stderr: stdout stays the answer alone, so a script reading a single prompt's answer
    /// isn't given usage text.
    /// </summary>
    private static void WriteUsageLine(string? line)
    {
        if (line is null) return;
        var previous = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Error.WriteLine(line);
        Console.Error.WriteLine();
        Console.ForegroundColor = previous;
    }

    /// <summary>Writes the reply as it streams (thinking in grey), with a one-line status for each tool call.</summary>
    private async Task StreamToConsoleAsync(Conversation conversation, string userInput,
        CancellationToken cancellationToken = default)
    {
        bool midLine = false, inThinking = false;
        // Every piece of every update, in the order they came, as one stream.
        var contents = engine.StreamPromptAsync(conversation, userInput, cancellationToken)
            .SelectMany(update => update.Contents);
        await foreach (var content in contents)
        {
            switch (content)
            {
                case TextReasoningContent { Text.Length: > 0 } thinking:
                    var previous = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write(thinking.Text);
                    Console.ForegroundColor = previous;
                    midLine = !thinking.Text.EndsWith('\n');
                    inThinking = true;
                    break;
                case TextContent { Text.Length: > 0 } text:
                    // Separates the thinking from the answer that follows it.
                    if (inThinking) Console.Write(midLine ? "\n\n" : "\n");
                    inThinking = false;
                    Console.Write(text.Text);
                    midLine = !text.Text.EndsWith('\n');
                    break;
                case FunctionCallContent call:
                    if (midLine) Console.WriteLine();
                    inThinking = false;
                    var asked = new ToolCallStatusHoles(call.Name, ToolCallText.Describe(call.Arguments));
                    WriteStatus(MessageText.Format(CliUserText.ToolCallStatus, asked));
                    midLine = false;
                    break;
                case FunctionResultContent result:
                    var outcome = result.Exception is null ? CliUserText.ToolDone : CliUserText.ToolFailed;
                    var ended = new ToolResultStatusHoles(MessageText.Format(outcome),
                        FirstLine(result.Result?.ToString()));
                    WriteStatus(MessageText.Format(CliUserText.ToolResultStatus, ended));
                    break;
            }
        }

        // A status line in grey, cut short when it runs long.
        static void WriteStatus(string line)
        {
            var previous = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(line.Length > MaxStatusChars ? line[..MaxStatusChars] + "..." : line);
            Console.ForegroundColor = previous;
        }

        // The first line of a tool's answer, trimmed; nothing when it gave none.
        static string FirstLine(string? text) => (text ?? string.Empty).Split('\n', 2)[0].Trim();
    }
}
