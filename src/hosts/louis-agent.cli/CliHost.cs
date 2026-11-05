using LouisAgent.Core.Agent.Catalog;
using LouisAgent.Core.Hosting;

namespace LouisAgent.Cli;

/// <summary>
/// A chat with the agent in your terminal. You say which packs and identities your app has, and this does the rest.
/// </summary>
/// <remarks>
/// Composition Root: your program's entry point is the one place that wires the app, so this library never picks a pack
/// itself. Without that it would have to reference someone's project, and a package can't.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0074-the-folders-say-what-ships.md">ADR 0074: framework and implementation are split</seealso>
public static class CliHost
{
    /// <summary>Answers the prompt you gave and stops, or opens a chat if you gave none.</summary>
    public static async Task RunAsync(string[] args, AgentCatalog catalog)
    {
        var host = AgentHost.Build(catalog);
        var chat = new ConsoleChat(host.Engine);

        if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
        {
            await chat.RunSinglePromptAsync(args[0]);
            return;
        }

        await chat.RunAsync();
    }
}
