namespace louis_agent.core.tools;

using System.ComponentModel;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Azure DevOps work items for one project (DEVOPS_ORGANIZATION / DEVOPS_PROJECT, optional DEVOPS_TEAM). Sprints are
/// iterations under the project, or under the team when one is set; an empty sprint means the team's current sprint.
/// </summary>
public sealed class DevOpsTools
{
    private readonly string _apiKey;
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _teamBaseUrl;
    private readonly string _project;
    private readonly string _iterationRoot;

    public DevOpsTools(string apiKey, string organization = "", string project = "", string? team = null)
    {
        _apiKey = apiKey;
        _project = project;
        _baseUrl = $"https://dev.azure.com/{Uri.EscapeDataString(organization)}/{Uri.EscapeDataString(project)}/_apis";
        _teamBaseUrl = string.IsNullOrWhiteSpace(team)
            ? _baseUrl
            : $"https://dev.azure.com/{Uri.EscapeDataString(organization)}/{Uri.EscapeDataString(project)}/{Uri.EscapeDataString(team)}/_apis";
        // Iteration paths are written with doubled backslashes in the WIQL text, as the queries below always have been.
        _iterationRoot = string.IsNullOrWhiteSpace(team) ? project : $"{project}\\\\{team}";
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        SetupAuthHeader();
    }

    private void SetupAuthHeader()
    {
        string auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($":{_apiKey}"));
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {auth}");
    }

    [Description("Run an arbitrary WIQL (Work Item Query Language) query against this project's work items and return the raw JSON result. Unlike the sprint-scoped convenience tools below (GetTasksBySprint, GetBugsBySprint, GetWorkItemsByState, etc.), this query is not automatically scoped to any sprint or team — write WHERE clauses for [System.IterationPath], [System.TeamProject], etc. yourself if you need that. Use this when the question doesn't fit any of the pre-built sprint tools (cross-sprint queries, custom fields, unusual filter combinations); for 'items of type X in sprint Y', prefer the matching convenience tool, since it already resolves the current sprint and handles the common filters correctly.")]
    public async Task<string> QueryWorkItems(
        [Description("WIQL query string (e.g., SELECT [System.Id], [System.Title] FROM workitems WHERE...)")] string wiqlQuery)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return "Error: DEVOPS_API_KEY is not set.";
        }

        return await ExecuteWiqlQuery(wiqlQuery);
    }

    [Description("List every work item of any type in one sprint assigned to a specific person, or to the caller (the DEVOPS_API_KEY's owner, via the WIQL @Me macro) when assignee is left empty. Leave sprint empty to use the team's current sprint, resolved automatically; call GetAvailableSprints first if you need to target a specific named sprint instead. Not filtered by work item type or state — pair with GetWorkItemsByState or GetOpenIssuesBySprint if you only want a subset.")]
    public async Task<string> GetWorkItemsByAssignee(
        [Description("Sprint name (e.g., 'Sprint 42'); leave empty for the team's current sprint")] string sprint = "",
        [Description("Assignee display name or email; leave empty for yourself (the API key's owner)")] string assignee = "")
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return "Error: DEVOPS_API_KEY is not set.";
        }

        var (iteration, sprintError) = await ResolveIterationPathAsync(sprint);
        if (sprintError is not null)
        {
            return sprintError;
        }

        string query = $"SELECT [System.Id], [System.Title], [System.State], [System.WorkItemType] " +
                      $"FROM workitems WHERE [System.TeamProject]='{_project}' " +
                      $"AND [System.IterationPath] Under '{iteration}' " +
                      (string.IsNullOrWhiteSpace(assignee) ? "AND [System.AssignedTo] = @Me" : $"AND [System.AssignedTo]='{assignee}'");
        return await ExecuteWiqlQuery(query);
    }

    [Description("List all work items with type 'User Story' in one sprint, across every state and assignee. Leave sprint empty to use the team's current sprint, resolved automatically; an explicit name must match an iteration returned by GetAvailableSprints. Use GetTasksBySprint or GetBugsBySprint instead for those other work item types, or QueryWorkItems for a custom type filter.")]
    public async Task<string> GetUserStoriesBySprint(
        [Description("Sprint name (e.g., 'Sprint 42'); leave empty for the team's current sprint")] string sprint = "")
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return "Error: DEVOPS_API_KEY is not set.";
        }

        var (iteration, sprintError) = await ResolveIterationPathAsync(sprint);
        if (sprintError is not null)
        {
            return sprintError;
        }

        string query = $"SELECT [System.Id], [System.Title], [System.State], [System.AssignedTo] " +
                      $"FROM workitems WHERE [System.TeamProject]='{_project}' " +
                      $"AND [System.WorkItemType]='User Story' " +
                      $"AND [System.IterationPath] Under '{iteration}'";
        return await ExecuteWiqlQuery(query);
    }

    [Description("List all work items with type 'Task' in one sprint, across every state and assignee. Leave sprint empty to use the team's current sprint, resolved automatically; an explicit name must match an iteration returned by GetAvailableSprints. Use GetUserStoriesBySprint or GetBugsBySprint instead for those other work item types, or QueryWorkItems for a custom type filter.")]
    public async Task<string> GetTasksBySprint(
        [Description("Sprint name (e.g., 'Sprint 42'); leave empty for the team's current sprint")] string sprint = "")
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return "Error: DEVOPS_API_KEY is not set.";
        }

        var (iteration, sprintError) = await ResolveIterationPathAsync(sprint);
        if (sprintError is not null)
        {
            return sprintError;
        }

        string query = $"SELECT [System.Id], [System.Title], [System.State], [System.AssignedTo] " +
                      $"FROM workitems WHERE [System.TeamProject]='{_project}' " +
                      $"AND [System.WorkItemType]='Task' " +
                      $"AND [System.IterationPath] Under '{iteration}'";
        return await ExecuteWiqlQuery(query);
    }

    [Description("List all work items with type 'Bug' in one sprint, across every state and assignee. Leave sprint empty to use the team's current sprint, resolved automatically; an explicit name must match an iteration returned by GetAvailableSprints. Use GetUserStoriesBySprint or GetTasksBySprint instead for those other work item types, or QueryWorkItems for a custom type filter.")]
    public async Task<string> GetBugsBySprint(
        [Description("Sprint name (e.g., 'Sprint 42'); leave empty for the team's current sprint")] string sprint = "")
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return "Error: DEVOPS_API_KEY is not set.";
        }

        var (iteration, sprintError) = await ResolveIterationPathAsync(sprint);
        if (sprintError is not null)
        {
            return sprintError;
        }

        string query = $"SELECT [System.Id], [System.Title], [System.State], [System.AssignedTo] " +
                      $"FROM workitems WHERE [System.TeamProject]='{_project}' " +
                      $"AND [System.WorkItemType]='Bug' " +
                      $"AND [System.IterationPath] Under '{iteration}'";
        return await ExecuteWiqlQuery(query);
    }

    [Description("Fetch the full field set for exactly one work item by its numeric ID, including fields the sprint-scoped list tools don't return (description, history links, relations) via Azure DevOps' $expand=all. Use this once you already have an ID from one of the list/search tools and need its complete detail, not to search for items — it doesn't accept partial IDs or names. Returns an error string if the ID doesn't exist or the request fails, rather than throwing.")]
    public async Task<string> GetWorkItemById(
        [Description("Work item ID")] string id)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return "Error: DEVOPS_API_KEY is not set.";
        }

        try
        {
            string url = $"{_baseUrl}/wit/workitems/{id}?api-version=7.0&$expand=all";
            using var response = await _httpClient.GetAsync(url);
            string content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return $"Error {response.StatusCode}: {content}";
            }

            var json = JsonDocument.Parse(content);
            return JsonSerializer.Serialize(json.RootElement, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return $"Error retrieving work item {id}: {ex.Message}";
        }
    }

    [Description("List all work items in one sprint whose System.State exactly matches the given string (e.g. 'Active', 'Resolved', 'Closed') — a case-sensitive match against whatever your project's workflow states are actually named. Leave sprint empty to use the team's current sprint, resolved automatically. For the common 'Active or New' case use GetOpenIssuesBySprint instead, which already combines both states.")]
    public async Task<string> GetWorkItemsByState(
        [Description("Work item state (e.g., 'Active', 'Resolved', 'Closed')")] string state,
        [Description("Sprint name (e.g., 'Sprint 42'); leave empty for the team's current sprint")] string sprint = "")
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return "Error: DEVOPS_API_KEY is not set.";
        }

        var (iteration, sprintError) = await ResolveIterationPathAsync(sprint);
        if (sprintError is not null)
        {
            return sprintError;
        }

        string query = $"SELECT [System.Id], [System.Title], [System.State], [System.WorkItemType], [System.AssignedTo] " +
                      $"FROM workitems WHERE [System.TeamProject]='{_project}' " +
                      $"AND [System.State]='{state}' " +
                      $"AND [System.IterationPath] Under '{iteration}'";
        return await ExecuteWiqlQuery(query);
    }

    [Description("Search for work items in one sprint whose title contains the given keyword (a WIQL Contains match, not an exact or regex match), sorted by ID descending and capped at maxResults (1-100, default 20). Leave sprint empty to use the team's current sprint, resolved automatically. Use this for free-text title lookups; use GetWorkItemById instead once you already know the exact ID.")]
    public async Task<string> FindWorkItemsByTitle(
        [Description("Keyword to search in titles")] string keyword,
        [Description("Sprint name (e.g., 'Sprint 42'); leave empty for the team's current sprint")] string sprint = "",
        [Description("Max results to return (1-100, default 20)")] int maxResults = 20)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return "Error: DEVOPS_API_KEY is not set.";
        }

        var (iteration, sprintError) = await ResolveIterationPathAsync(sprint);
        if (sprintError is not null)
        {
            return sprintError;
        }

        maxResults = Math.Clamp(maxResults, 1, 100);
        string query = $"SELECT [System.Id], [System.Title], [System.State], [System.WorkItemType], [System.AssignedTo] " +
                      $"FROM workitems WHERE [System.TeamProject]='{_project}' " +
                      $"AND [System.Title] Contains '{keyword}' " +
                      $"AND [System.IterationPath] Under '{iteration}' " +
                      $"ORDER BY [System.Id] DESC";
        return await ExecuteWiqlQuery(query);
    }

    [Description("List work items in one sprint whose state is 'Active' or 'New', ordered by most recently changed first — the common 'what's still open' view. Leave sprint empty to use the team's current sprint, resolved automatically. For a different or single state use GetWorkItemsByState instead.")]
    public async Task<string> GetOpenIssuesBySprint(
        [Description("Sprint name (e.g., 'Sprint 42'); leave empty for the team's current sprint")] string sprint = "")
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return "Error: DEVOPS_API_KEY is not set.";
        }

        var (iteration, sprintError) = await ResolveIterationPathAsync(sprint);
        if (sprintError is not null)
        {
            return sprintError;
        }

        string query = $"SELECT [System.Id], [System.Title], [System.State], [System.WorkItemType], [System.AssignedTo] " +
                      $"FROM workitems WHERE [System.TeamProject]='{_project}' " +
                      $"AND ([System.State]='Active' OR [System.State]='New') " +
                      $"AND [System.IterationPath] Under '{iteration}' " +
                      $"ORDER BY [System.ChangedDate] DESC";
        return await ExecuteWiqlQuery(query);
    }

    [Description("List work items in one sprint whose System.AssignedTo field is empty — items nobody currently owns. Leave sprint empty to use the team's current sprint, resolved automatically. Use GetWorkItemsByAssignee instead to find items assigned to someone specific.")]
    public async Task<string> GetUnassignedWorkItems(
        [Description("Sprint name (e.g., 'Sprint 42'); leave empty for the team's current sprint")] string sprint = "")
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return "Error: DEVOPS_API_KEY is not set.";
        }

        var (iteration, sprintError) = await ResolveIterationPathAsync(sprint);
        if (sprintError is not null)
        {
            return sprintError;
        }

        string query = $"SELECT [System.Id], [System.Title], [System.State], [System.WorkItemType] " +
                      $"FROM workitems WHERE [System.TeamProject]='{_project}' " +
                      $"AND [System.AssignedTo] = '' " +
                      $"AND [System.IterationPath] Under '{iteration}'";
        return await ExecuteWiqlQuery(query);
    }

    [Description("List work items in one sprint whose work item type is literally named 'Blocker'. This only returns results if the project actually has a work item type called 'Blocker' — most Azure DevOps process templates don't ship one by default, so an empty result here may mean there's no such type rather than no blockers. For a project that tracks blocking relationships via tags or links instead, use QueryWorkItems with a custom WIQL filter.")]
    public async Task<string> GetBlockersBySprint(
        [Description("Sprint name (e.g., 'Sprint 42'); leave empty for the team's current sprint")] string sprint = "")
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return "Error: DEVOPS_API_KEY is not set.";
        }

        var (iteration, sprintError) = await ResolveIterationPathAsync(sprint);
        if (sprintError is not null)
        {
            return sprintError;
        }

        string query = $"SELECT [System.Id], [System.Title], [System.State], [System.AssignedTo] " +
                      $"FROM workitems WHERE [System.TeamProject]='{_project}' " +
                      $"AND [System.WorkItemType]='Blocker' " +
                      $"AND [System.IterationPath] Under '{iteration}'";
        return await ExecuteWiqlQuery(query);
    }

    [Description("List work items in one sprint whose System.ChangedDate falls within the last `days` days (default 7), ordered most-recently-changed first. Leave sprint empty to use the team's current sprint, resolved automatically. Not filtered by type or state — combine with GetWorkItemsByState if you only want recent changes to, say, open bugs.")]
    public async Task<string> GetRecentlyModifiedWorkItems(
        [Description("Number of days to look back (default 7)")] int days = 7,
        [Description("Sprint name (e.g., 'Sprint 42'); leave empty for the team's current sprint")] string sprint = "")
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return "Error: DEVOPS_API_KEY is not set.";
        }

        var (iteration, sprintError) = await ResolveIterationPathAsync(sprint);
        if (sprintError is not null)
        {
            return sprintError;
        }

        string cutoffDate = DateTime.Now.AddDays(-days).ToString("yyyy-MM-dd");
        string query = $"SELECT [System.Id], [System.Title], [System.State], [System.ChangedDate], [System.AssignedTo] " +
                      $"FROM workitems WHERE [System.TeamProject]='{_project}' " +
                      $"AND [System.ChangedDate] >= '{cutoffDate}' " +
                      $"AND [System.IterationPath] Under '{iteration}' " +
                      $"ORDER BY [System.ChangedDate] DESC";
        return await ExecuteWiqlQuery(query);
    }

    [Description("List every work item in one sprint, ordered by state then work item type, as a flat result — a quick 'what does this sprint contain' overview rather than a pre-aggregated count-by-state summary (grouping/counting is left to the caller). Leave sprint empty to use the team's current sprint, resolved automatically.")]
    public async Task<string> GetSprintSummary(
        [Description("Sprint name (e.g., 'Sprint 42'); leave empty for the team's current sprint")] string sprint = "")
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return "Error: DEVOPS_API_KEY is not set.";
        }

        var (iteration, sprintError) = await ResolveIterationPathAsync(sprint);
        if (sprintError is not null)
        {
            return sprintError;
        }

        string query = $"SELECT [System.Id], [System.Title], [System.State], [System.WorkItemType], [System.AssignedTo] " +
                      $"FROM workitems WHERE [System.TeamProject]='{_project}' " +
                      $"AND [System.IterationPath] Under '{iteration}' " +
                      $"ORDER BY [System.State], [System.WorkItemType]";
        return await ExecuteWiqlQuery(query);
    }

    [Description("List the names of every iteration (sprint) configured for the team, sorted alphabetically — not scoped to any single sprint itself. Call this first when you need a specific sprint name to pass to the other sprint-scoped tools, or when one of them reports it couldn't find a current sprint. Returns 'No sprints found.' rather than an error if the team has none configured.")]
    public async Task<string> GetAvailableSprints()
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return "Error: DEVOPS_API_KEY is not set.";
        }

        try
        {
            string url = $"{_teamBaseUrl}/work/teamsettings/iterations?api-version=7.0";
            using var response = await _httpClient.GetAsync(url);
            string content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return $"Error {response.StatusCode}: {content}";
            }

            var json = JsonDocument.Parse(content);
            var sprints = new List<string>();

            if (json.RootElement.TryGetProperty("value", out var values))
            {
                foreach (var item in values.EnumerateArray())
                {
                    if (item.TryGetProperty("name", out var name))
                    {
                        sprints.Add(name.GetString() ?? "");
                    }
                }
            }

            return sprints.Count == 0
                ? "No sprints found."
                : "Available sprints:\n" + string.Join("\n", sprints.OrderBy(s => s));
        }
        catch (Exception ex)
        {
            return $"Error retrieving sprints: {ex.Message}";
        }
    }

    [Description("List every work item in one sprint with just its ID, assignee, state and type — intended for tallying how work is distributed across the team, though the caller does the grouping/counting, not this tool. Leave sprint empty to use the team's current sprint, resolved automatically. Use GetWorkItemsByAssignee instead if you only care about one specific person.")]
    public async Task<string> GetTeamWorkloadBySprint(
        [Description("Sprint name (e.g., 'Sprint 42'); leave empty for the team's current sprint")] string sprint = "")
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return "Error: DEVOPS_API_KEY is not set.";
        }

        var (iteration, sprintError) = await ResolveIterationPathAsync(sprint);
        if (sprintError is not null)
        {
            return sprintError;
        }

        string query = $"SELECT [System.Id], [System.AssignedTo], [System.State], [System.WorkItemType] " +
                      $"FROM workitems WHERE [System.TeamProject]='{_project}' " +
                      $"AND [System.IterationPath] Under '{iteration}'";

        return await ExecuteWiqlQuery(query);
    }

    /// <summary>
    /// The WIQL iteration path for a sprint name, or the team's current sprint when the name is empty.
    /// Returns an error message instead of a path when the current sprint can't be found.
    /// </summary>
    private async Task<(string Path, string? Error)> ResolveIterationPathAsync(string sprint)
    {
        if (!string.IsNullOrWhiteSpace(sprint))
        {
            return ($"{_iterationRoot}\\\\{sprint}", null);
        }

        const string hint = "Pass a sprint name (GetAvailableSprints lists them).";
        try
        {
            using var response = await _httpClient.GetAsync($"{_teamBaseUrl}/work/teamsettings/iterations?$timeframe=current&api-version=7.0");
            string content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                return ("", $"Error {response.StatusCode} looking up the current sprint: {content} {hint}");
            }

            using var json = JsonDocument.Parse(content);
            if (json.RootElement.TryGetProperty("value", out var values) && values.GetArrayLength() > 0 &&
                values[0].TryGetProperty("path", out var path) && path.GetString() is { Length: > 0 } iterationPath)
            {
                return (iterationPath.Replace("\\", "\\\\"), null);
            }

            return ("", $"Error: the team has no current sprint. {hint}");
        }
        catch (Exception ex)
        {
            return ("", $"Error looking up the current sprint: {ex.Message} {hint}");
        }
    }

    private async Task<string> ExecuteWiqlQuery(string wiqlQuery)
    {
        try
        {
            var payload = new { query = wiqlQuery };
            string json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            string url = $"{_baseUrl}/wit/wiql?api-version=7.0";
            using var response = await _httpClient.PostAsync(url, content);
            string responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return $"Error {response.StatusCode}: {responseContent}";
            }

            var jsonDoc = JsonDocument.Parse(responseContent);
            return JsonSerializer.Serialize(jsonDoc.RootElement, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return $"Error executing WIQL query: {ex.Message}";
        }
    }

    [Description("Create a new work item of the given type (e.g. 'Task', 'Bug', 'User Story') with a title and optional description — this mutates the project by adding a real work item, not a dry run or preview. The type must be a valid work item type in this project or the request fails. Returns the created item's ID and title on success, or an error string on failure; use UpdateWorkItem afterward to set any other field (assignee, state, iteration, etc.) that this tool doesn't take.")]
    public async Task<string> CreateWorkItem(
        [Description("Work item type (e.g., 'Task', 'Bug', 'User Story')")] string workItemType,
        [Description("Title for the work item")] string title,
        [Description("Description (optional)")] string description = "")
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return "Error: DEVOPS_API_KEY is not set.";
        }

        try
        {
            var patches = new List<object>
            {
                new { op = "add", path = "/fields/System.Title", value = title }
            };

            if (!string.IsNullOrWhiteSpace(description))
            {
                patches.Add(new { op = "add", path = "/fields/System.Description", value = description });
            }

            string json = JsonSerializer.Serialize(patches);
            var content = new StringContent(json, Encoding.UTF8, "application/json-patch+json");

            string url = $"{_baseUrl}/wit/workitems/${workItemType}?api-version=7.0";
            using var response = await _httpClient.PostAsync(url, content);
            string responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return $"Error {response.StatusCode}: {responseContent}";
            }

            var jsonDoc = JsonDocument.Parse(responseContent);
            if (jsonDoc.RootElement.TryGetProperty("id", out var idElement))
            {
                return $"Created {workItemType} #{idElement.GetInt32()} - {title}";
            }

            return $"Created work item: {responseContent}";
        }
        catch (Exception ex)
        {
            return $"Error creating work item: {ex.Message}";
        }
    }

    [Description("Update exactly one field on an existing work item by ID using a JSON Patch 'add' operation, which also overwrites a field that already has a value. fieldName must be the Azure DevOps reference name (e.g. 'System.State', 'System.AssignedTo'), not the display label, and fieldValue is written as-is with no validation against the field's allowed values. This mutates the live work item with no preview or undo; call UpdateWorkItem again with the previous value to revert.")]
    public async Task<string> UpdateWorkItem(
        [Description("Work item ID")] string id,
        [Description("Field name (e.g., 'System.State', 'System.AssignedTo')")] string fieldName,
        [Description("New field value")] string fieldValue)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return "Error: DEVOPS_API_KEY is not set.";
        }

        try
        {
            var patches = new[]
            {
                new { op = "add", path = $"/fields/{fieldName}", value = fieldValue }
            };

            string json = JsonSerializer.Serialize(patches);
            var content = new StringContent(json, Encoding.UTF8, "application/json-patch+json");

            string url = $"{_baseUrl}/wit/workitems/{id}?api-version=7.0";
            using var response = await _httpClient.PatchAsync(url, content);
            string responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return $"Error {response.StatusCode}: {responseContent}";
            }

            return $"Updated work item #{id}: {fieldName} = {fieldValue}";
        }
        catch (Exception ex)
        {
            return $"Error updating work item: {ex.Message}";
        }
    }
}
