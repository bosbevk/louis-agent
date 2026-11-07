namespace LouisAgent.Packs.DevOps;

/// <summary>The DevOps pack's own settings. Without the first three, the toolset stays off.</summary>
public sealed record DevOpsOptions(string? ApiKey = null, string? Organization = null, string? Project = null,
    string? Team = null)
{
    /// <summary>The settings from the environment, once the host has loaded its settings files.</summary>
    public static DevOpsOptions FromEnvironment() => new(Environment.GetEnvironmentVariable("DEVOPS_API_KEY"),
        Environment.GetEnvironmentVariable("DEVOPS_ORGANIZATION"), Environment.GetEnvironmentVariable("DEVOPS_PROJECT"),
        Environment.GetEnvironmentVariable("DEVOPS_TEAM"));

    /// <summary>The settings still blank, by name.</summary>
    public IReadOnlyList<string> Missing() =>
    [
        .. new[] { ("DEVOPS_API_KEY", ApiKey), ("DEVOPS_ORGANIZATION", Organization), ("DEVOPS_PROJECT", Project) }
            .Where(setting => string.IsNullOrWhiteSpace(setting.Item2)).Select(setting => setting.Item1)
    ];
}
