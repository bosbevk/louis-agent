using System.ComponentModel;

namespace LouisAgent.Composition.Packs.Screens.Acting;

/// <summary>What a screen says for itself (ADR 0009), for a person, never the model.</summary>
public enum ScreenMessage
{
    [Description("This screen doesn't offer that.")]
    NotOffered
}
