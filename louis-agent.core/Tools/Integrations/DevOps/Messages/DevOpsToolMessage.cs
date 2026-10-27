
using System.ComponentModel;

namespace LouisAgent.Core.Tools.Integrations.DevOps.Messages;
// The message enums (docs/CLAUDE.md, *Messages and logging*). Each member is named for what happened, and its
// [Description] is the text, with named holes filled by MessageText.Format (or by the structured logger). Members are added
// as code is written or moved; the refactor after F4 moves the existing messages here.

/// <summary>Strings for the Azure DevOps integration, returned to the model: what went wrong and what to do instead.</summary>
public enum DevOpsToolMessage
{
    [Description("Error: DEVOPS_API_KEY is not set.")]
    DevOpsKeyNotSet,

    [Description("Error {Status}: {Content}")]
    DevOpsHttpError,

    [Description("Error retrieving work item {Id}: {Reason}")]
    DevOpsWorkItemFailed,

    [Description("Error retrieving sprints: {Reason}")]
    DevOpsSprintsFailed,

    [Description(
        "Error {Status} looking up the current sprint: {Content} Pass a sprint name (GetAvailableSprints lists them).")]
    DevOpsCurrentSprintHttpError,

    [Description("Error: the team has no current sprint. Pass a sprint name (GetAvailableSprints lists them).")]
    DevOpsNoCurrentSprint,

    [Description("Error looking up the current sprint: {Reason} Pass a sprint name (GetAvailableSprints lists them).")]
    DevOpsCurrentSprintFailed,

    [Description("Error executing WIQL query: {Reason}")]
    DevOpsQueryFailed,

    [Description("Error creating work item: {Reason}")]
    DevOpsCreateFailed,

    [Description("Error updating work item: {Reason}")]
    DevOpsUpdateFailed,

    [Description("No sprints found.")] NoSprints,

    [Description("Created {Type} #{Id} - {Title}")]
    WorkItemCreated,

    [Description("Created work item: {Response}")]
    WorkItemCreatedRaw,

    [Description("Updated work item #{Id}: {Field} = {Value}")]
    WorkItemUpdated,
}
