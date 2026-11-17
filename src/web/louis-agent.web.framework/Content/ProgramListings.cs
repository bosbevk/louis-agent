namespace LouisAgent.Web.Framework.Content;

/// <summary>
/// The programs the framework's pages show, each the real lines from a program in the repo, so a page never shows code
/// that no longer runs: a test holds each to its file.
/// </summary>
public static class ProgramListings
{
    /// <summary>The call that starts the coding agent in the terminal, from its own <c>Program.cs</c>.</summary>
    public const string RunTheCodingAgent =
        """
        await CliHost.RunAsync(args, CodingCatalog.Create(), new HostServices(WebAccess.Create, OtlpTelemetry.Instance,
            Providers: [AnthropicProvider.Instance, OllamaProvider.Instance, OpenAiCompatibleProvider.Instance]));
        """;
}
