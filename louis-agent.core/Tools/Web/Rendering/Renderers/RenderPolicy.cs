namespace LouisAgent.Core.Tools.Web.Rendering.Renderers;

using System.Net;
using LouisAgent.Core.Guardrails;
using PuppeteerSharp;

/// <summary>
/// What one request a rendered page makes gets (F32 step 2): a browser follows redirects and loads scripts, data and
/// frames on its own, so the address rule <see cref="WebTools.FetchUrl"/> keeps for each hop is kept here for each
/// request. Anything not public (the Puls stack, its database, the cloud metadata address; <see cref="SsrfGuard"/> decides,
/// as for FetchUrl), any scheme but http(s) and data, or a host that doesn't resolve is refused; images, fonts and media
/// are skipped, since the text doesn't need them; the rest is allowed. Pure, so it's tested without a browser.
/// </summary>
internal static class RenderPolicy
{
    internal static async Task<RenderVerdict> DecideAsync(Uri url, ResourceType type, Func<string, CancellationToken, Task<IPAddress[]>> resolve)
    {
        // Inline data reaches no address.
        if (url.Scheme == "data") return RenderVerdict.Allow;
        if (url.Scheme is not ("http" or "https")) return RenderVerdict.Refuse;
        try
        {
            await SsrfGuard.EnsurePublicHostAsync(url, resolve);
        }
        catch (Exception ex) when (ex is BlockedAddressException or HttpRequestException)
        {
            return RenderVerdict.Refuse;
        }
        return type is ResourceType.Image or ResourceType.Font or ResourceType.Media ? RenderVerdict.Skip : RenderVerdict.Allow;
    }
}
