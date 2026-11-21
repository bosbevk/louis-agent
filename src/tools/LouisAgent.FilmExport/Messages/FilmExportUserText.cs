using System.ComponentModel;
using LouisAgent.Composition.Messages;
using LouisAgent.FilmExport.Messages.Holes;

namespace LouisAgent.FilmExport.Messages;

/// <summary>What the export tool tells the person running it.</summary>
public enum FilmExportUserText
{
    [Description("Unknown film '{Name}'. Supported: {Supported}.")]
    [Holes(typeof(UnknownFilmHoles))]
    UnknownFilm,

    [Description("No louis-agent-solution.sln above {Folder}: run this from inside the repo.")]
    [Holes(typeof(RepoNotFoundHoles))]
    RepoNotFound,

    [Description("The site didn't answer at {Address}.")]
    SiteNotReady,

    [Description("The encoder failed. Is Docker running? {Output}")]
    EncodeFailed,

    [Description("{Film}: rendering {Frames} frames")]
    [Holes(typeof(RenderingHoles))]
    Rendering,

    [Description("{Film}: encoding")]
    [Holes(typeof(EncodingHoles))]
    Encoding,

    [Description("{Film}: {Path}, {Megabytes} MB")]
    [Holes(typeof(ExportedHoles))]
    Exported,

    [Description("The share card: {Path}")]
    [Holes(typeof(CardWrittenHoles))]
    CardWritten,
}
