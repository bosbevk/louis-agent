
using System.ComponentModel;

namespace LouisAgent.Core.Tools.Integrations.Paymo.Messages;
// The message enums (docs/CLAUDE.md, *Messages and logging*). Each member is named for what happened, and its
// [Description] is the text, with named holes filled by MessageText.Format (or by the structured logger). Members are added
// as code is written or moved; the refactor after F4 moves the existing messages here.

/// <summary>Strings for the Paymo integration, returned to the model: what went wrong and what to do instead.</summary>
public enum PaymoToolMessage
{
    [Description("ERROR: Paymo API key not configured.")]
    PaymoKeyNotConfigured,

    [Description("ERROR: Failed to {Action} (HTTP {Status})")]
    PaymoRequestHttpFailed,

    [Description("ERROR: Failed to {Action}: {Reason}")]
    PaymoRequestFailed,

    [Description("ERROR: {Item} not found (HTTP {Status})")]
    PaymoNotFound,

    [Description("ERROR: Paymo API key not configured. Set PAYMO_API_KEY environment variable.")]
    PaymoKeyNotConfiguredSetIt,

    [Description("ERROR: Task name or ID is required.")]
    PaymoTaskRequired,

    [Description("ERROR: Could not find task '{Task}'. Try using the numeric task ID instead.")]
    PaymoTaskNotFoundByName,

    [Description("ERROR: Duration must be greater than zero.")]
    PaymoDurationNotPositive,

    [Description("ERROR: Failed to create time entry (HTTP {Status}). Response: {Response}")]
    PaymoCreateEntryFailed,

    [Description("ERROR: {Reason}")] PaymoError,

    [Description("ERROR: Unexpected error: {Reason}")]
    PaymoUnexpectedError,

    [Description("Task name or ID cannot be empty.")]
    PaymoTaskEmpty,

    [Description("Failed to resolve task ID for '{Task}': {Reason}")]
    PaymoTaskResolveFailed,

    [Description("Must provide either start_time/end_time OR hours parameter.")]
    PaymoTimeOrHoursRequired,

    [Description("Invalid start_time format: '{Time}'. Use HH:MM (e.g., '08:00').")]
    PaymoStartTimeInvalid,

    [Description("Invalid end_time format: '{Time}'. Use HH:MM (e.g., '09:00').")]
    PaymoEndTimeInvalid,

    [Description("Invalid hours value: '{Hours}'. Use a positive number (e.g., '2' or '1.5').")]
    PaymoHoursInvalid,

    [Description("✓ Time entry {Id} deleted successfully")]
    PaymoEntryDeleted,

    [Description("✓ Time entry created successfully")]
    PaymoEntryCreated,
}
