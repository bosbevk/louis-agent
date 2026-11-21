namespace LouisAgent.Web.Components.Site.Product.Scene;

/// <summary>How far the page in the window has come: as it was, the right card shown, the request drawn, or sent.</summary>
public enum SceneStage
{
    /// <summary>The page as it was; a step that changes nothing.</summary>
    None,

    /// <summary>The agent opened the right card: it stands forward and the others step back.</summary>
    Shown,

    /// <summary>The agent drew the request as a card over the page.</summary>
    Card,

    /// <summary>The request went to the owner.</summary>
    Sent
}
