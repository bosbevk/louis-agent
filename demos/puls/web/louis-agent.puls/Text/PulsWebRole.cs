using LouisAgent.Choices;

namespace LouisAgent.Puls.Text;

/// <summary>The roles Puls's web app has words for (ADR 0043: a host keeps its own enum of the names it uses): the cook's work phases ("Soek bestanddele", "Bou resep"). A member's wire name is the role's name in a session's stream.</summary>
public enum PulsWebRole
{
    [WireName("puls-cook")] Cook,
}
