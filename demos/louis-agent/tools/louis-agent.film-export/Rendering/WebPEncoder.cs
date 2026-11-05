using LouisAgent.Composition.Messages;
using LouisAgent.Composition.Tools;
using LouisAgent.FilmExport.Messages;

namespace LouisAgent.FilmExport.Rendering;

/// <summary>Turns a folder of frames into an animated WebP, with libwebp's own tool from a Docker image.</summary>
/// <remarks>
/// libwebp's <c>img2webp</c>, because it can force keyframes and ffmpeg can't. Between keyframes the encoder treats a
/// small change as no change, so a slow fade on low-contrast text never gets written, and a card keeps the last card's
/// words (ADR 0077). From an image so nobody has to install it, and it stays a tool a person runs, never part of a
/// product. The arguments go as a list, never through a shell (ADR 0004).
/// </remarks>
public static class WebPEncoder
{
    /// <summary>Where the encoder's image is described, from the repo's root.</summary>
    public const string Dockerfile = "demos/louis-agent/docker/images/Dockerfile.film-encoder";

    private const string Image = "louis-agent-film-encoder";

    /// <summary>A long film takes minutes, so this only stops one that's stuck.</summary>
    private static readonly TimeSpan Limit = TimeSpan.FromMinutes(40);

    /// <summary>Encodes the frames, and throws with the encoder's own words if it can't.</summary>
    public static void ToWebP(string repoRoot, string framesFolder, string output, int frames, int fps, int quality)
    {
        // Instant once it's built: Docker keeps the image.
        string dockerfile = Path.Combine(repoRoot, Dockerfile);
        Run(["build", "-q", "-t", Image, "-f", dockerfile, Path.GetDirectoryName(dockerfile)!], repoRoot);

        Run(Arguments(framesFolder, output, frames, fps, quality), framesFolder);
    }

    /// <summary>What <c>docker</c> is called with: the frames mounted read-only, and the output's folder.</summary>
    public static IReadOnlyList<string>
        Arguments(string framesFolder, string output, int frames, int fps, int quality) =>
    [
        "run", "--rm", "-v", $"{Forward(framesFolder)}:/frames:ro",
        "-v", $"{Forward(Path.GetDirectoryName(output)!)}:/out", "-w", "/frames",
        Image, "img2webp", "-loop", "0", "-lossy", "-q", $"{quality}", "-m", "4",
        "-kmax", $"{fps / 2}", "-kmin", $"{fps / 4}", "-d", $"{(int)Math.Round(1000.0 / fps)}",
        .. Enumerable.Range(0, frames).Select(FilmRenderer.FrameName),
        "-o", $"/out/{Path.GetFileName(output)}",
    ];

    /// <summary>Runs <c>docker</c>, and throws with its own words if it fails.</summary>
    private static void Run(IReadOnlyList<string> arguments, string folder)
    {
        var result = ProcessRunner.Run("docker", arguments, folder, Limit);
        if (result.ExitCode != 0)
            throw new InvalidOperationException(
                MessageText.Format(FilmExportUserText.EncodeFailed, new { result.Output }));
    }

    /// <summary>A path with forward slashes, the way Docker reads a mount on either system.</summary>
    private static string Forward(string path) => path.Replace('\\', '/');
}
