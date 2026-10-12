namespace LouisAgent.Cli;

using LouisAgent.Core.Agent;
using LouisAgent.Core.Usage;
using Microsoft.Extensions.AI;
using LouisAgent.Core.Messages;
using LouisAgent.Cli.Messages;

/// <summary>
/// The CLI's chat on the console: the interactive loop and single-prompt mode. It maps the engine's stream to the
/// terminal (the answer on stdout, thinking and tool status in grey, the usage line on stderr); the engine holds no
/// console code (docs/CLAUDE.md, Project layout, rule 4: hosts stay thin).
/// </summary>
internal sealed class ConsoleChat(AgentEngine engine)
{
    /// <summary>Run the interactive chat loop.</summary>
    public async Task RunAsync()
    {
        var history = engine.NewHistory();
        // One CLI run is one session in the usage ledger.
        string session = $"cli_{Guid.NewGuid():N}";
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
            if (userInput is null || userInput.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(userInput))
            {
                continue;
            }

            // Approval commands and /usage come from the user only; they never reach the model.
            if (engine.HandleUserCommand(userInput, engine.RecordsUsage ? sessionUsage.Records : null) is { } commandReply)
            {
                Console.WriteLine($"\n{commandReply}\n");
                continue;
            }

            using var cancellation = new CancellationTokenSource();
            Volatile.Write(ref turn, cancellation);
            using var usage = UsageScope.Begin(host: "cli", session: session, turn: ++turns);
            try
            {
                Console.Write(MessageText.Format(CliUserText.AssistantPrompt));
                await StreamToConsoleAsync(history, userInput, cancellation.Token);
                Console.WriteLine("\n");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine(MessageText.Format(CliUserText.Stopped));
            }
            catch (Exception ex)
            {
                Console.WriteLine(MessageText.Format(CliUserText.TurnError, new { Reason = ex.Message }));
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
        using var usage = UsageScope.Begin(host: "cli", session: $"cli_{Guid.NewGuid():N}", turn: 1);
        try
        {
            await StreamToConsoleAsync(engine.NewHistory(), userInput);
            Console.WriteLine();
            // One prompt is the whole session, so the line has no session total.
            if (usage.Records.Count > 0) WriteUsageLine(UsageText.Line(usage.Records, display: engine.Display));
        }
        catch (Exception ex)
        {
            Console.WriteLine(MessageText.Format(CliUserText.PromptError, new { Reason = ex.Message }));
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
    private async Task StreamToConsoleAsync(List<ChatMessage> history, string userInput, CancellationToken cancellationToken = default)
    {
        bool midLine = false, inThinking = false;
        await foreach (var update in engine.StreamPromptAsync(history, userInput, cancellationToken))
        {
            foreach (var content in update.Contents)
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
                        if (inThinking)
                        {
                            // Separates the thinking from the answer that follows it.
                            Console.Write(midLine ? "\n\n" : "\n");
                            inThinking = false;
                        }
                        Console.Write(text.Text);
                        midLine = !text.Text.EndsWith('\n');
                        break;
                    case FunctionCallContent call:
                        if (midLine) Console.WriteLine();
                        inThinking = false;
                        WriteStatus(MessageText.Format(CliUserText.ToolCallStatus, new { call.Name, Arguments = AgentEngine.DescribeArguments(call.Arguments) }));
                        midLine = false;
                        break;
                    case FunctionResultContent result:
                        WriteStatus(MessageText.Format(CliUserText.ToolResultStatus, new { Outcome = (result.Exception is null ? MessageText.Format(CliUserText.ToolDone) : MessageText.Format(CliUserText.ToolFailed)), Result = FirstLine(result.Result?.ToString()) }));
                        break;
                }
            }
        }

        static void WriteStatus(string line)
        {
            var previous = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(line.Length > 160 ? line[..160] + "..." : line);
            Console.ForegroundColor = previous;
        }

        static string FirstLine(string? text) => (text ?? string.Empty).Split('\n', 2)[0].Trim();
    }
}
