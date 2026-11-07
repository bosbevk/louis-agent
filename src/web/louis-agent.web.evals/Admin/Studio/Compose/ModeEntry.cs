namespace LouisAgent.Web.Evals.Admin.Studio.Compose;

/// <summary>
/// One mode of an eval being composed: its name, the role its sessions open with, and its toolsets as the person
/// types them, commas between.
/// </summary>
/// <remarks>A class: the form's inputs bind to it and change it as the person types (ADR 0040).</remarks>
public sealed class ModeEntry
{
    /// <summary>The mode's name.</summary>
    public string Name { get; set; } = "";

    /// <summary>The role its sessions open with.</summary>
    public string Role { get; set; } = "";

    /// <summary>Its toolsets, commas between.</summary>
    public string Toolsets { get; set; } = "";
}
