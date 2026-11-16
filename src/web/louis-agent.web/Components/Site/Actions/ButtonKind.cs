namespace LouisAgent.Web.Components.Site.Actions;

/// <summary>Which of the theme's buttons a link is drawn as: its own two, or a third party's.</summary>
public enum ButtonKind
{
    /// <summary>The main button, the theme's press: what a site most wants a visitor to do.</summary>
    Main,

    /// <summary>The second button, the theme's quiet press: another way on.</summary>
    Quiet,

    /// <summary>A WhatsApp chat, drawn as WhatsApp's guidelines ask (ADR 0102).</summary>
    WhatsApp,

    /// <summary>A LinkedIn profile, drawn as LinkedIn's guidelines ask (ADR 0102).</summary>
    LinkedIn
}
