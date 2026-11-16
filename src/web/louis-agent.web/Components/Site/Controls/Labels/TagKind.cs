namespace LouisAgent.Web.Components.Site.Controls.Labels;

/// <summary>Whose a tag is: plain, the visitor's, or the agent's.</summary>
public enum TagKind
{
    /// <summary>A plain label, such as a tool's name.</summary>
    Plain,

    /// <summary>What the visitor picked, in their colour.</summary>
    You,

    /// <summary>What the agent shows, in the theme's colour.</summary>
    Agent
}
