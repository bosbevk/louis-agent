using LouisAgent.Composition.Messages;
using LouisAgent.Composition.Tools;
using LouisAgent.FilmExport.Messages;

namespace LouisAgent.FilmExport.Rendering;

/// <summary>Turns a folder of frames into an animated WebP, with ffmpeg from a Docker image.</summary>
/// <remarks>
/// From an image so nobody has to install ffmpeg, and it stays a tool a person runs, never part of a product. Pinned by
/// its digest, so a re-export months later uses the same ffmpeg. The arguments go as a list, never through a shell
/// (ADR 0004).
/// </remarks>
public static class FfmpegEncoder
{
    /// <summary>ffmpeg 7.1 on Alpine, which has the animated WebP encoder.</summary>
    private const string Image = "jrottenberg/ffmpeg@sha256:8ec1ee1f6a0fcd37c97725827b6b7832795c9596e3439b8da56d7700d61ae778";

    /// <summary>A long film at its slowest setting takes minutes, so this only stops one that's stuck.</summary>
    private static readonly TimeSpan Limit = TimeSpan.FromMinutes(40);

    /// <summary>Encodes the frames, and throws with ffmpeg's own words if it can't.</summary>
    public static void ToWebP(string framesFolder, string output, int fps, int quality)
    {
        var result = ProcessRunner.Run("docker", Arguments(framesFolder, output, fps, quality), framesFolder, Limit);
        if (result.ExitCode != 0)
            throw new InvalidOperationException(
                MessageText.Format(FilmExportUserText.EncodeFailed, new { result.Output }));
    }

    /// <summary>What <c>docker</c> is called with: the frames mounted read-only, and the output's folder.</summary>
    public static IReadOnlyList<string> Arguments(string framesFolder, string output, int fps, int quality) =>
    [
        "run", "--rm", "-v", $"{Forward(framesFolder)}:/frames:ro",
        "-v", $"{Forward(Path.GetDirectoryName(output)!)}:/out",
        Image, "-y", "-v", "error", "-framerate", $"{fps}", "-i", $"/frames/{FilmRenderer.FramePattern}", "-loop", "0",
        "-c:v", "libwebp_anim", "-quality", $"{quality}", "-compression_level", "6", $"/out/{Path.GetFileName(output)}",
    ];

    /// <summary>A path with forward slashes, the way Docker reads a mount on either system.</summary>
    private static string Forward(string path) => path.Replace('\\', '/');
}
