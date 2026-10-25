namespace LouisAgent.Core.Tools.Git;

using System.ComponentModel;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Tools.Git.Running;
using static LouisAgent.Core.Tools.Git.Running.GitArguments;
using LouisAgent.Core.Tools.Git.Messages;

/// <summary>
/// The git toolset's configuration (S, CL15: one of its five classes). <c>SetConfig</c> sets only <c>user.name</c> and
/// <c>user.email</c> (FX19): other keys (core.fsmonitor, core.sshCommand, core.hooksPath, alias.*) make git run commands.
/// </summary>
public sealed class GitConfigTools(GitRunner git)
{
    private static readonly string[] ConfigKeysAllowed = ["user.name", "user.email"];

    [Description("Read git configuration values (git config). With no key, returns the full configuration list (git config --list); with a key such as user.name, returns just that value. Read-only; use SetConfig to change user.name or user.email.")]
    public string GetConfig(
        [Description("Config key (e.g., 'user.name'); if empty, shows all config")] string key = "")
    {
        if (string.IsNullOrWhiteSpace(key)) return git.Run(["config", "--list"]);
        return Refused(nameof(key), key) ?? git.Run(["config", "--get", EndOfOptions, key]);
    }

    [Description("Set user.name or user.email in the repository's local config (git config), so commits from this workspace carry that identity. Only those two keys: others (core.fsmonitor, core.sshCommand, core.hooksPath, alias.*) can make git run commands, so they are refused. Use GetConfig first to check the current value before overwriting it.")]
    public string SetConfig(
        [Description("Config key: user.name or user.email")] string key,
        [Description("Config value")] string value)
    {
        string name = key.Trim();
        if (!ConfigKeysAllowed.Contains(name, StringComparer.OrdinalIgnoreCase))
            return MessageText.Format(GitToolMessage.GitConfigKeyRefused, new { Key = key, Allowed = string.Join(" and ", ConfigKeysAllowed) });
        return git.Run(["config", EndOfOptions, name.ToLowerInvariant(), value]);
    }
}
