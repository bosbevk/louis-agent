namespace LouisAgent.Core.Tools.Web.Rendering.Providers;

/// <summary>What happens to one request a rendered page makes (F32 step 2): <see cref="RenderPolicy"/> decides.</summary>
public enum RenderVerdict
{
    /// <summary>A public http(s) address the text may need.</summary>
    Allow,

    /// <summary>An image, a font or media: public, but nothing the text needs.</summary>
    Skip,

    /// <summary>An internal address, another scheme, or a host that doesn't resolve.</summary>
    Refuse,
}
