namespace LouisAgent.Packs.Docs;

/// <summary>The docs pack's one setting, read once by the composition root: where the docs are. Unset, the pack stays off.</summary>
/// <remarks>
/// A plain options object with a reader that takes the variable lookup, as <c>ShopPricesOptions</c> is, so a test
/// needs no real environment.
/// </remarks>
public sealed class DocsOptions
{
    /// <summary>The setting's name, which the toolset reports as missing while it is unset.</summary>
    public const string RootSetting = "DOCS_ROOT";

    /// <summary>
    /// DOCS_ROOT: the folder the docs are under, which the tools never leave; on the PC the repo's docs, in the image
    /// the copy it carries.
    /// </summary>
    public string? Root { get; init; }

    /// <summary>Reads the setting; the variable lookup is replaceable for tests.</summary>
    public static DocsOptions FromEnvironment(Func<string, string?>? getVariable = null)
    {
        getVariable ??= Environment.GetEnvironmentVariable;
        string? root = getVariable(RootSetting);
        return new DocsOptions { Root = string.IsNullOrWhiteSpace(root) ? null : root.Trim() };
    }
}
