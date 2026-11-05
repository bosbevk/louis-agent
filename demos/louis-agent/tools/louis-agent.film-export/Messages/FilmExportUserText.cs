using System.ComponentModel;

namespace LouisAgent.FilmExport.Messages;

/// <summary>What the export tool tells the person running it.</summary>
public enum FilmExportUserText
{
    [Description("Unknown film '{Name}'. Supported: {Supported}.")]
    UnknownFilm,

    [Description("No louis-agent-solution.sln above {Folder}: run this from inside the repo.")]
    RepoNotFound,

    [Description("The site didn't answer at {Address}.")]
    SiteNotReady,

    [Description("ffmpeg failed. Is Docker running? {Output}")]
    EncodeFailed,

    [Description("{Film}: rendering {Frames} frames")]
    Rendering,

    [Description("{Film}: encoding")]
    Encoding,

    [Description("{Film}: {Path}, {Megabytes} MB")]
    Exported,
}
