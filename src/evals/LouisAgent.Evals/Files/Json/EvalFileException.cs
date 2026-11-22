namespace LouisAgent.Evals.Files.Json;

/// <summary>
/// An eval file refused at its first fault: where it is, as a JSON path such as
/// <c>$.conversations[2].turns[0].facts[1]</c>, and why, in the message that starts with that place.
/// </summary>
/// <param name="place">The fault's place in the file, as a JSON path.</param>
/// <param name="message">The whole refusal, the place first.</param>
/// <param name="inner">The serializer's own exception, for a fault of shape.</param>
public sealed class EvalFileException(string place, string message, Exception? inner = null)
    : Exception(message, inner)
{
    /// <summary>The fault's place in the file, as a JSON path.</summary>
    public string Place { get; } = place;
}
