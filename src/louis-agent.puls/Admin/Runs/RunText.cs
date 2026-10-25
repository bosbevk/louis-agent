namespace LouisAgent.Puls.Admin.Runs;

/// <summary>How the portal writes a run's id where a full one is too long.</summary>
public static class RunText
{
    private const int ShortLength = 8;

    /// <summary>The first characters of an id: enough to tell two runs apart on a line.</summary>
    public static string Short(string id) => id.Length <= ShortLength ? id : id[..ShortLength];
}
