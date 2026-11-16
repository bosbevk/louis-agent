namespace LouisAgent.Web.Components.Site.Showcase;

/// <summary>Which small film a feature card plays, each showing one thing the agent does.</summary>
public enum FeatureFilm
{
    /// <summary>It opens the right page: three tiles, the one that fits lifts.</summary>
    Show,

    /// <summary>It draws what was asked for: a card's lines drawn in.</summary>
    Draw,

    /// <summary>It asks before it sends: a question and three choices, one pressed.</summary>
    Ask,

    /// <summary>It shows its work: an answer that turns over to show what it called.</summary>
    Flip
}
