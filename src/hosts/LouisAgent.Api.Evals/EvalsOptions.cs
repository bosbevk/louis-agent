using LouisAgent.Core.Config;

namespace LouisAgent.Api.Evals;

/// <summary>
/// The evals' settings, read once when the host starts: where runs are kept, which model judges, and where the
/// agent is asked, with the instance's own key.
/// </summary>
/// <param name="DataFolder">The site's data folder; runs are kept in <c>evals/runs/</c> under it.</param>
/// <param name="JudgeModel">The judge's model; none takes the host's own.</param>
/// <param name="ApiUrl">Where the agent is asked; none is the program's own address.</param>
/// <param name="ApiKey">The instance's key, sent with every ask; none on a host that asks for none.</param>
public sealed record EvalsOptions(string? DataFolder, string? JudgeModel, Uri? ApiUrl, string? ApiKey)
{
    /// <summary>The setting that names the site's data folder, as the CV's store reads it.</summary>
    public const string DataFolderSetting = "SITE_DATA_DIRECTORY";

    /// <summary>The setting that names the judge's model.</summary>
    public const string JudgeModelSetting = "EVALS_JUDGE_MODEL";

    /// <summary>The setting that names where the agent is asked.</summary>
    public const string ApiUrlSetting = "EVALS_API_URL";

    /// <summary>The setting that holds the instance's key.</summary>
    public const string ApiKeySetting = "AGENT_API_KEY";

    /// <summary>Where runs are kept, or none on a host with no data folder.</summary>
    public string? RunsFolder => DataFolder is null ? null : Path.Combine(DataFolder, "evals", "runs");

    /// <summary>The settings as the environment gives them; a blank one is none.</summary>
    public static EvalsOptions FromEnvironment(Func<string, string?>? getVariable = null)
    {
        var read = getVariable ?? Environment.GetEnvironmentVariable;
        string? Named(string setting) => string.IsNullOrWhiteSpace(read(setting)) ? null : read(setting)!.Trim();

        Uri? apiUrl = Uri.TryCreate(Named(ApiUrlSetting), UriKind.Absolute, out var url) ? url : null;
        return new EvalsOptions(Named(DataFolderSetting), Named(JudgeModelSetting), apiUrl, Named(ApiKeySetting));
    }

    /// <summary>The judge's model settings: the host's provider and key, with the judge's model when one is set.</summary>
    public LlmOptions JudgeLlm(LlmOptions host) => new()
    {
        Provider = host.Provider,
        Model = JudgeModel ?? host.Model,
        Endpoint = host.Endpoint,
        ApiKey = host.ApiKey,
        WorkspaceId = host.WorkspaceId
    };
}
