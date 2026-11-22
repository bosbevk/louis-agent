using System.ComponentModel;
using LouisAgent.Composition.Messages;
using LouisAgent.FilmExport.Messages.Holes;
using LouisAgent.FilmExport.Messages.Holes.Cards;

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

    [Description("{Site}: {Page}. {Heading}")]
    [Holes(typeof(CardDescriptionHoles))]
    CardDescription,

    [Description("The page's title '{Title}' doesn't hold the site's name '{Site}', so its card would print the "
                 + "whole title.")]
    [Holes(typeof(TitleWithoutSiteHoles))]
    TitleWithoutSite,
}
