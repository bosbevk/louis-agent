namespace LouisAgent.Diagrams.Icons;

/// <summary>
/// Each icon's path data, from Tabler Icons (MIT, its notice in <c>LICENSE-tabler-icons.txt</c> beside this file), in
/// Tabler's 24-unit box: line drawings meant to be stroked, round at the ends and the joins, never filled.
/// </summary>
/// <remarks>
/// Copied in as data, with no package and no icon font (Louis's pick P3, as F134 does for the showroom; ADR 0030,
/// ADR 0055): an SVG file must hold everything it draws, and a few paths are all a picture needs.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0030-build-or-adopt.md">ADR 0030: build or adopt</seealso>
public static class IconPaths
{
    /// <summary>The paths that draw <paramref name="icon"/>.</summary>
    public static IReadOnlyList<string> Of(DiagramIcon icon) => icon switch
    {
        DiagramIcon.Chip =>
        [
            "M5 5m0 1a1 1 0 0 1 1 -1h12a1 1 0 0 1 1 1v12a1 1 0 0 1 -1 1h-12a1 1 0 0 1 -1 -1z", "M9 9h6v6h-6z",
            "M3 10h2", "M3 14h2", "M10 3v2", "M14 3v2", "M21 10h-2", "M21 14h-2", "M14 21v-2", "M10 21v-2"
        ],
        DiagramIcon.User => ["M8 7a4 4 0 1 0 8 0a4 4 0 0 0 -8 0", "M6 21v-2a4 4 0 0 1 4 -4h4a4 4 0 0 1 4 4v2"],
        DiagramIcon.Users =>
        [
            "M9 7m-4 0a4 4 0 1 0 8 0a4 4 0 1 0 -8 0", "M3 21v-2a4 4 0 0 1 4 -4h4a4 4 0 0 1 4 4v2",
            "M16 3.13a4 4 0 0 1 0 7.75", "M21 21v-2a4 4 0 0 0 -3 -3.85"
        ],
        DiagramIcon.Box =>
            ["M12 3l8 4.5l0 9l-8 4.5l-8 -4.5l0 -9l8 -4.5", "M12 12l8 -4.5", "M12 12l0 9", "M12 12l-8 -4.5"],
        DiagramIcon.Server =>
        [
            "M3 4m0 3a3 3 0 0 1 3 -3h12a3 3 0 0 1 3 3v2a3 3 0 0 1 -3 3h-12a3 3 0 0 1 -3 -3z",
            "M3 12m0 3a3 3 0 0 1 3 -3h12a3 3 0 0 1 3 3v2a3 3 0 0 1 -3 3h-12a3 3 0 0 1 -3 -3z", "M7 8l0 .01",
            "M7 16l0 .01"
        ],
        DiagramIcon.Database =>
        [
            "M12 6m-8 0a8 3 0 1 0 16 0a8 3 0 1 0 -16 0", "M4 6v6a8 3 0 0 0 16 0v-6", "M4 12v6a8 3 0 0 0 16 0v-6"
        ],
        DiagramIcon.World =>
        [
            "M3 12a9 9 0 1 0 18 0a9 9 0 0 0 -18 0", "M3.6 9h16.8", "M3.6 15h16.8", "M11.5 3a17 17 0 0 0 0 18",
            "M12.5 3a17 17 0 0 1 0 18"
        ],
        DiagramIcon.Message =>
        [
            "M8 9h8", "M8 13h6",
            "M18 4a3 3 0 0 1 3 3v8a3 3 0 0 1 -3 3h-5l-5 3v-3h-2a3 3 0 0 1 -3 -3v-8a3 3 0 0 1 3 -3h12z"
        ],
        DiagramIcon.Shield => ["M12 3a12 12 0 0 0 8.5 3a12 12 0 0 1 -8.5 15a12 12 0 0 1 -8.5 -15a12 12 0 0 0 8.5 -3"],
        DiagramIcon.Code => ["M7 8l-4 4l4 4", "M17 8l4 4l-4 4", "M14 4l-4 16"],
        DiagramIcon.Folder => ["M5 4h4l3 3h7a2 2 0 0 1 2 2v8a2 2 0 0 1 -2 2h-14a2 2 0 0 1 -2 -2v-11a2 2 0 0 1 2 -2"],
        DiagramIcon.File =>
        [
            "M14 3v4a1 1 0 0 0 1 1h4", "M17 21h-10a2 2 0 0 1 -2 -2v-14a2 2 0 0 1 2 -2h7l5 5v11a2 2 0 0 1 -2 2z",
            "M9 9l1 0", "M9 13l6 0", "M9 17l6 0"
        ],
        DiagramIcon.Browser =>
        [
            "M4 4m0 1a1 1 0 0 1 1 -1h14a1 1 0 0 1 1 1v14a1 1 0 0 1 -1 1h-14a1 1 0 0 1 -1 -1z", "M4 8h16", "M8 4v4"
        ],
        DiagramIcon.Checks => ["M7 12l5 5l10 -10", "M2 12l5 5m5 -5l5 -5"],
        DiagramIcon.Chart =>
        [
            "M3 13a1 1 0 0 1 1 -1h4a1 1 0 0 1 1 1v6a1 1 0 0 1 -1 1h-4a1 1 0 0 1 -1 -1z",
            "M15 9a1 1 0 0 1 1 -1h4a1 1 0 0 1 1 1v10a1 1 0 0 1 -1 1h-4a1 1 0 0 1 -1 -1z",
            "M9 5a1 1 0 0 1 1 -1h4a1 1 0 0 1 1 1v14a1 1 0 0 1 -1 1h-4a1 1 0 0 1 -1 -1z", "M4 20h14"
        ],
        DiagramIcon.Cloud =>
        [
            "M6.657 18c-2.572 0 -4.657 -2.007 -4.657 -4.483c0 -2.475 2.085 -4.482 4.657 -4.482c.393 -1.762 1.794 " +
            "-3.2 3.675 -3.773c1.88 -.572 3.956 -.193 5.444 1c1.488 1.19 2.162 3.007 1.77 4.769h.99c1.913 0 3.464 " +
            "1.56 3.464 3.486c0 1.927 -1.551 3.487 -3.465 3.487h-11.878"
        ],
        DiagramIcon.Settings =>
        [
            "M10.325 4.317c.426 -1.756 2.924 -1.756 3.35 0a1.724 1.724 0 0 0 2.573 1.066c1.543 -.94 3.31 .826 2.37 " +
            "2.37a1.724 1.724 0 0 0 1.065 2.572c1.756 .426 1.756 2.924 0 3.35a1.724 1.724 0 0 0 -1.066 2.573c.94 " +
            "1.543 -.826 3.31 -2.37 2.37a1.724 1.724 0 0 0 -2.572 1.065c-.426 1.756 -2.924 1.756 -3.35 0a1.724 " +
            "1.724 0 0 0 -2.573 -1.066c-1.543 .94 -3.31 -.826 -2.37 -2.37a1.724 1.724 0 0 0 -1.065 -2.572c-1.756 " +
            "-.426 -1.756 -2.924 0 -3.35a1.724 1.724 0 0 0 1.066 -2.573c-.94 -1.543 .826 -3.31 2.37 -2.37c1 .608 " +
            "2.296 .07 2.572 -1.065z",
            "M9 12a3 3 0 1 0 6 0a3 3 0 0 0 -6 0"
        ],
        DiagramIcon.Branch =>
        [
            "M7 18m-2 0a2 2 0 1 0 4 0a2 2 0 1 0 -4 0", "M7 6m-2 0a2 2 0 1 0 4 0a2 2 0 1 0 -4 0",
            "M17 6m-2 0a2 2 0 1 0 4 0a2 2 0 1 0 -4 0", "M7 8l0 8", "M9 18h6a2 2 0 0 0 2 -2v-5", "M14 14l3 -3l3 3"
        ],
        DiagramIcon.Mail =>
        [
            "M3 7a2 2 0 0 1 2 -2h14a2 2 0 0 1 2 2v10a2 2 0 0 1 -2 2h-14a2 2 0 0 1 -2 -2v-10z", "M3 7l9 6l9 -6"
        ],
        DiagramIcon.Search => ["M10 10m-7 0a7 7 0 1 0 14 0a7 7 0 1 0 -14 0", "M21 21l-6 -6"],
        DiagramIcon.Book =>
        [
            "M3 19a9 9 0 0 1 9 0a9 9 0 0 1 9 0", "M3 6a9 9 0 0 1 9 0a9 9 0 0 1 9 0", "M3 6l0 13", "M12 6l0 13",
            "M21 6l0 13"
        ],
        DiagramIcon.Terminal => ["M5 7l5 5l-5 5", "M12 19l7 0"],
        _ => []
    };
}
