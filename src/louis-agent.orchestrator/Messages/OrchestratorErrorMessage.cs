namespace LouisAgent.Orchestrator.Messages;

using System.ComponentModel;

/// <summary>The orchestrator's exception messages.</summary>
public enum OrchestratorErrorMessage
{
    [Description("No runbooks for '{Service}': {Directory} does not exist.")]
    NoRunbooks,

    [Description("ORCHESTRATOR_SERVICE_ROOT is required: the service's git repository.")]
    ServiceRootRequired,
}
