using LouisAgent.Core.Messages;
using LouisAgent.Evals.Messages;

namespace LouisAgent.Evals.Api;

/// <summary>The API answered a request with a status that is not a success (F18 step 5.1): the status and the message the API gave.</summary>
public sealed class EvalApiException(int status, string reason)
    : Exception(MessageText.Format(EvalErrorMessage.ApiRefused, new { Status = status, Reason = reason }))
{
    public int Status { get; } = status;
}
