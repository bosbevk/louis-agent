using LouisAgent.Web.Components.Imports;

namespace LouisAgent.Web.Components.Packs;

/// <summary>A component that can draw an upload: the page hands it the upload's form, the rest is its own.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0071-packs-take-data-uploads.md">ADR 0071: packs take data uploads</seealso>
public interface IImportView
{
    /// <summary>The upload, with the files chosen and the report so far.</summary>
    ImportForm Form { get; set; }
}
