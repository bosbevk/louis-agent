namespace LouisAgent.Core.Hosting.EnvFiles;

using System.Text.RegularExpressions;
using dotenv.net;
using LouisAgent.Core.Config;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;
using Microsoft.Extensions.Logging;
using LouisAgent.Core.Hosting.Messages;

/// <summary>The .env files a host loads before it reads its options (S, CL15, from AgentHost), and the rule a profile's name follows.</summary>
public static partial class HostEnvironment
{
    private static readonly ILogger Log = AgentLogging.LoggerFactory.CreateLogger(typeof(HostEnvironment));

    /// <summary>
    /// Loads config/.env.secrets and config/.env (plus a legacy ./.env), searching upward from the working
    /// directory so the CLI works from any folder in the repo, then the LLM profile config/.env.{LLM_PROFILE}
    /// (.env.anthropic, .env.ollama). Variables already set in the process (shell, Docker env_file) win; each file
    /// only fills what the ones before it left unset, so the order is secrets, .env, then the profile.
    /// </summary>
    public static void Load() => Load(System.Environment.CurrentDirectory);

    internal static void Load(string startDirectory)
    {
        var files = new List<string>();
        string? configDir = EnvFile.FindConfigDirectory(startDirectory);
        if (configDir is not null)
        {
            files.Add(Path.Combine(configDir, ".env.secrets"));
            files.Add(Path.Combine(configDir, ".env"));
        }
        files.Add(Path.Combine(startDirectory, ".env"));
        LoadFiles(files);

        // Read after .env is loaded, since that is where it is usually set.
        if (System.Environment.GetEnvironmentVariable("LLM_PROFILE") is not { Length: > 0 } profile || configDir is null) return;
        string profileFile = Path.Combine(configDir, $".env.{profile}");
        if (!IsProfileName(profile) || !File.Exists(profileFile))
        {
            Log.Log(LogLevel.Warning, HostingLogMessage.LlmProfileMissing, new { Profile = profile, File = $"config/.env.{profile}" });
            return;
        }
        LoadFiles([profileFile]);

        static void LoadFiles(IEnumerable<string> paths) => DotEnv.Load(options: new DotEnvOptions(
            ignoreExceptions: true,
            envFilePaths: paths.Where(File.Exists).ToArray(),
            overwriteExistingVars: false));
    }

    /// <summary>Profile names are file-name parts (anthropic, ollama), never paths.</summary>
    public static bool IsProfileName(string name) => LlmProfileName().IsMatch(name);

    [GeneratedRegex("^[a-z0-9-]+$")]
    private static partial Regex LlmProfileName();
}
