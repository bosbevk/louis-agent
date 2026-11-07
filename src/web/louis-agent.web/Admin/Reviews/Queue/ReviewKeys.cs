namespace LouisAgent.Web.Admin.Reviews.Queue;

/// <summary>
/// The Review page's keys (F53.3): 1 is pass, 2 fail, 3 unsure, as labelling tools number their choices.
/// </summary>
public static class ReviewKeys
{
    /// <summary>The verdict a key stands for, in the panel's words, or none for any other key.</summary>
    public static string? VerdictOf(string key) => key switch
    {
        "1" => "pass",
        "2" => "fail",
        "3" => "unsure",
        _ => null,
    };
}
