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

    [Description("Search work items using a WIQL query (Work Item Query Language).")]
    public async Task<string> QueryWorkItems(
        [Description("WIQL query string (e.g., SELECT [System.Id], [System.Title] FROM workitems WHERE...)")] string wiqlQuery)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return "Error: DEVOPS_API_KEY is not set.";
        }

        return await ExecuteWiqlQuery(wiqlQuery);
    }

    [Description("Get all work items in a sprint assigned to a specific person.")]
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

    [Description("Get all user stories in a sprint.")]
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

    [Description("Get all tasks in a sprint.")]
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

    [Description("Get all bugs in a sprint.")]
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

    [Description("Get detailed information about a specific work item.")]
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

    [Description("Get all work items in a specific state (e.g., Active, Resolved, Closed).")]
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

    [Description("Find work items by title keyword.")]
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

    [Description("Get all open issues (Active or New state) in a sprint.")]
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

    [Description("Get work items with no assignee (unassigned) in a sprint.")]
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

    [Description("Get blockers and dependencies in a sprint.")]
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

    [Description("Get work items that changed in the last N days.")]
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

    [Description("Get a summary of work items in a sprint grouped by state.")]
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

    [Description("Get list of available sprints.")]
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

    [Description("Get team members and their workload in a sprint.")]
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

    [Description("Create a new work item.")]
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

    [Description("Update a work item field.")]
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
