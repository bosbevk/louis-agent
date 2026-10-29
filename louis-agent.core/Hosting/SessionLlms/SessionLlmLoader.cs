using LouisAgent.Choices;

using dotenv.net;
using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting.EnvFiles;
using LouisAgent.Core.Llm;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Tools.Agents;
using Microsoft.Extensions.Logging;
using LouisAgent.Core.Hosting.Messages;

namespace LouisAgent.Core.Hosting.SessionLlms;
/// <summary>The models a session may name besides the instance's own (F30; S, CL15, from AgentHost): their profiles read, the minions' choices checked.</summary>
public static class SessionLlmLoader
{
    private static readonly ILogger Log = AgentLogging.LoggerFactory.CreateLogger(typeof(SessionLlmLoader));

    /// <summary>The settings an LLM profile file never holds, taken from the environment instead (.env.secrets).</summary>
    private static readonly string[] SecretLlmSettings = ["LLM_API_KEY", "ANTHROPIC_API_KEY", "ANTHROPIC_WORKSPACE_ID"];

    /// <summary>
    /// Each profile in <c>LLM_SESSION_PROFILES</c> read from <c>config/.env.{name}</c> into a dictionary, so the process
    /// environment, and with it the instance's own model, never changes. Only the API key settings come from
    /// <paramref name="environment"/>; anything else a profile leaves out takes its default, not the instance's value. A
    /// listed profile without its file stops start-up.
    /// </summary>
    internal static IReadOnlyList<SessionLlm> Load(AgentOptions agent, ILlmClientFactory clientFactory,
        string? configDirectory,
        Func<string, string?> environment)
    {
        var llms = new List<SessionLlm>();
        foreach (string name in agent.SessionLlmProfiles)
        {
            if (!HostEnvironment.IsProfileName(name))
                throw new SessionLlmException(MessageText.Format(HostingErrorMessage.SessionLlmNameInvalid,
                    new { Name = name }));
            string file = Path.Combine(configDirectory ?? "", $".env.{name}");
            if (configDirectory is null || !File.Exists(file))
                throw new SessionLlmException(MessageText.Format(HostingErrorMessage.SessionLlmFileMissing,
                    new { Name = name, File = $"config/.env.{name}" }));

            var settings = DotEnv.Read(new DotEnvOptions(ignoreExceptions: false, envFilePaths: [file]));
            var llm = LlmOptions.FromEnvironment(key => settings.TryGetValue(key, out string? value) ? value
                : SecretLlmSettings.Contains(key) ? environment(key) : null);
            var model = new SessionLlm(name, clientFactory.Create(llm), llm.ResolveSupportsTools(),
                clientFactory.CreateUsageMapper(llm),
                llm.ResolveThinking());
            Log.Log(LogLevel.Information, HostingLogMessage.SessionLlmLoaded, new
            {
                Name = name,
                Llm = llm,
                Tools = model.SupportsTools ? "on" : "off",
                Thinking = model.Thinking?.Wire() ?? "off",
            });
            llms.Add(model);
        }

        return llms;
    }

    /// <summary>
    /// Every model <c>MINION_LLMS</c> names is a profile in <c>LLM_SESSION_PROFILES</c> or <c>instance</c> (F30); another
    /// stops start-up (<see cref="SessionLlmException"/>), so a typo never leaves a minion with no model at run time.
    /// </summary>
    internal static void CheckMinionLlms(AgentOptions agent)
    {
        foreach (var (role, models) in agent.MinionLlms)
            foreach (string model in models)
                if (model != MinionTools.InstanceLlm && !agent.SessionLlmProfiles.Contains(model, StringComparer.Ordinal))
                    throw new SessionLlmException(MessageText.Format(HostingErrorMessage.MinionLlmUnknown, new
                    {
                        Name = model,
                        Role = role,
                        Available = agent.SessionLlmProfiles.Count == 0
                            ? "none"
                            : string.Join(", ", agent.SessionLlmProfiles),
                    }));
    }
}
