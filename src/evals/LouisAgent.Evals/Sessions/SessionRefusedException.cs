using LouisAgent.Composition.Messages;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Messages.Holes.Sessions;

namespace LouisAgent.Evals.Sessions;

/// <summary>
/// The sessions API said no: to opening a session, to a message, or to closing one. It carries the status and the
/// API's own reason, so a run that stops on it can say why.
/// </summary>
/// <param name="status">The HTTP status the API answered with.</param>
/// <param name="reason">The API's own reason, or what came back when it gave none.</param>
public sealed class SessionRefusedException(int status, string reason) : Exception(MessageOf(status, reason))
{
    /// <summary>The HTTP status the API answered with.</summary>
    public int Status { get; } = status;

    /// <summary>The API's own reason.</summary>
    public string Reason { get; } = reason;

    /// <summary>The refusal said in the evals' words, with the status and the reason in it.</summary>
    private static string MessageOf(int status, string reason)
    {
        var refused = new RefusedHoles(status, reason);
        return MessageText.Format(SessionMessage.Refused, refused);
    }
}
