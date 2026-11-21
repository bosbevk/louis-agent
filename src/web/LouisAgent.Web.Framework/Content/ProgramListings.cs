namespace LouisAgent.Web.Framework.Content;

/// <summary>
/// The programs the framework's pages show, each the real lines from a program in the repo, so a page never shows code
/// that no longer runs: a test holds each to its file.
/// </summary>
public static class ProgramListings
{
    /// <summary>
    /// The coding agent's whole <c>Program.cs</c>, its usings and all: everything a program of your own needs to start
    /// an agent in the terminal.
    /// </summary>
    public const string CodingAgentProgram =
        """
        using LouisAgent.Core.Web;
        using LouisAgent.Core.Anthropic;
        using LouisAgent.Core.Ollama;
        using LouisAgent.Core.OpenAi;
        using LouisAgent.Core.Telemetry;
        using LouisAgent.Core.Hosting;
        using LouisAgent.Cli;
        using LouisAgent.Coding;

        // Usage: LouisAgent.CodingCli [prompt]. It loads what this demo's catalogue names.
        """ + "\n" + RunTheCodingAgent;

    /// <summary>The call that starts the coding agent in the terminal, from its own <c>Program.cs</c>.</summary>
    public const string RunTheCodingAgent =
        """
        await CliHost.RunAsync(args, CodingCatalog.Create(), new HostServices(WebAccess.Create, OtlpTelemetry.Instance,
            Providers: [AnthropicProvider.Instance, OllamaProvider.Instance, OpenAiCompatibleProvider.Instance]));
        """;
}
