namespace louis_agent.core.tools;

using System.ComponentModel;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;
using louis_agent.core.config;

/// <summary>
/// Native C# client for Paymo API, replacing bash curl scripts.
/// Handles task lookup, time entry creation, and error handling.
/// </summary>
public sealed class PaymoTools
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string BaseUrl = "https://app.paymoapp.com/api";

    public PaymoTools(AgentOptions options, HttpClient? httpClient = null)
    {
        _apiKey = options.PaymoApiKey ?? string.Empty;
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        _httpClient.DefaultRequestHeaders.Authorization ??= new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{_apiKey}:x")));
    }

    [Description("Search for tasks by name, status, or project.")]
    public async Task<string> SearchTasks(
        [Description("Task name or keyword to search for")] string query,
        [Description("Filter by status (e.g., 'active', 'completed')")] string status = "",
        [Description("Maximum results to return (1-100)")] int maxResults = 20)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return "ERROR: Paymo API key not configured.";
        }

        try
        {
            maxResults = Math.Clamp(maxResults, 1, 100);
            string url = $"{BaseUrl}/tasks";
            var filters = new List<string> { $"name__contains={Uri.EscapeDataString(query)}" };

            if (!string.IsNullOrWhiteSpace(status))
            {
                filters.Add($"status={Uri.EscapeDataString(status)}");
            }

            if (filters.Count > 0)
            {
                url += "?where=" + string.Join(" AND ", filters);
            }

            using var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return $"ERROR: Failed to search tasks (HTTP {(int)response.StatusCode})";
            }

            string content = await response.Content.ReadAsStringAsync();
            return FormatJsonResponse(content, maxResults);
        }
        catch (Exception ex)
        {
            return $"ERROR: Failed to search tasks: {ex.Message}";
        }
    }

    [Description("Get a list of all tasks, optionally filtered by project or status.")]
    public async Task<string> ListTasks(
        [Description("Filter by project ID (optional)")] string projectId = "",
        [Description("Filter by status: active, completed, on_hold (optional)")] string status = "",
        [Description("Maximum results (1-100)")] int maxResults = 30)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return "ERROR: Paymo API key not configured.";
        }

        try
        {
            maxResults = Math.Clamp(maxResults, 1, 100);
            string url = $"{BaseUrl}/tasks";
            var filters = new List<string>();

            if (!string.IsNullOrWhiteSpace(projectId))
            {
                filters.Add($"project_id={projectId}");
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                filters.Add($"status={Uri.EscapeDataString(status)}");
            }

            if (filters.Count > 0)
            {
                url += "?where=" + string.Join(" AND ", filters);
            }

            using var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return $"ERROR: Failed to list tasks (HTTP {(int)response.StatusCode})";
            }

            string content = await response.Content.ReadAsStringAsync();
            return FormatJsonResponse(content, maxResults);
        }
        catch (Exception ex)
        {
            return $"ERROR: Failed to list tasks: {ex.Message}";
        }
    }

    [Description("Get detailed information about a specific task.")]
    public async Task<string> GetTaskDetails(
        [Description("Task ID")] string taskId)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return "ERROR: Paymo API key not configured.";
        }

        try
        {
            using var response = await _httpClient.GetAsync($"{BaseUrl}/tasks/{taskId}");
            if (!response.IsSuccessStatusCode)
            {
                return $"ERROR: Task not found (HTTP {(int)response.StatusCode})";
            }

            string content = await response.Content.ReadAsStringAsync();
            return FormatJsonResponse(content);
        }
        catch (Exception ex)
        {
            return $"ERROR: Failed to get task details: {ex.Message}";
        }
    }

    [Description("Get list of all projects.")]
    public async Task<string> ListProjects(
        [Description("Filter by status: active, archived (optional)")] string status = "",
        [Description("Maximum results (1-100)")] int maxResults = 30)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return "ERROR: Paymo API key not configured.";
        }

        try
        {
            maxResults = Math.Clamp(maxResults, 1, 100);
            string url = $"{BaseUrl}/projects";

            if (!string.IsNullOrWhiteSpace(status))
            {
                url += $"?where=status={Uri.EscapeDataString(status)}";
            }

            using var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return $"ERROR: Failed to list projects (HTTP {(int)response.StatusCode})";
            }

            string content = await response.Content.ReadAsStringAsync();
            return FormatJsonResponse(content, maxResults);
        }
        catch (Exception ex)
        {
            return $"ERROR: Failed to list projects: {ex.Message}";
        }
    }

    [Description("Get detailed information about a specific project.")]
    public async Task<string> GetProjectDetails(
        [Description("Project ID")] string projectId)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return "ERROR: Paymo API key not configured.";
        }

        try
        {
            using var response = await _httpClient.GetAsync($"{BaseUrl}/projects/{projectId}");
            if (!response.IsSuccessStatusCode)
            {
                return $"ERROR: Project not found (HTTP {(int)response.StatusCode})";
            }

            string content = await response.Content.ReadAsStringAsync();
            return FormatJsonResponse(content);
        }
        catch (Exception ex)
        {
            return $"ERROR: Failed to get project details: {ex.Message}";
        }
    }

    [Description("Get list of all clients.")]
    public async Task<string> ListClients(
        [Description("Maximum results (1-100)")] int maxResults = 30)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return "ERROR: Paymo API key not configured.";
        }

        try
        {
            maxResults = Math.Clamp(maxResults, 1, 100);
            using var response = await _httpClient.GetAsync($"{BaseUrl}/clients");
            if (!response.IsSuccessStatusCode)
            {
                return $"ERROR: Failed to list clients (HTTP {(int)response.StatusCode})";
            }

            string content = await response.Content.ReadAsStringAsync();
            return FormatJsonResponse(content, maxResults);
        }
        catch (Exception ex)
        {
            return $"ERROR: Failed to list clients: {ex.Message}";
        }
    }

    [Description("Get list of team members/users.")]
    public async Task<string> ListUsers(
        [Description("Maximum results (1-100)")] int maxResults = 30)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return "ERROR: Paymo API key not configured.";
        }

        try
        {
            maxResults = Math.Clamp(maxResults, 1, 100);
            using var response = await _httpClient.GetAsync($"{BaseUrl}/users");
            if (!response.IsSuccessStatusCode)
            {
                return $"ERROR: Failed to list users (HTTP {(int)response.StatusCode})";
            }

            string content = await response.Content.ReadAsStringAsync();
            return FormatJsonResponse(content, maxResults);
        }
        catch (Exception ex)
        {
            return $"ERROR: Failed to list users: {ex.Message}";
        }
    }

    [Description("Get list of time entries, optionally filtered by date range or user.")]
    public async Task<string> ListTimeEntries(
        [Description("Start date in YYYY-MM-DD format (optional)")] string startDate = "",
        [Description("End date in YYYY-MM-DD format (optional)")] string endDate = "",
        [Description("Filter by user ID (optional)")] string userId = "",
        [Description("Maximum results (1-100)")] int maxResults = 50)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return "ERROR: Paymo API key not configured.";
        }

        try
        {
            maxResults = Math.Clamp(maxResults, 1, 100);
            string url = $"{BaseUrl}/entries";
            var filters = new List<string>();

            if (!string.IsNullOrWhiteSpace(startDate))
            {
                filters.Add($"date>='{startDate}'");
            }

            if (!string.IsNullOrWhiteSpace(endDate))
            {
                filters.Add($"date<='{endDate}'");
            }

            if (!string.IsNullOrWhiteSpace(userId))
            {
                filters.Add($"user_id={userId}");
            }

            if (filters.Count > 0)
            {
                url += "?where=" + string.Join(" AND ", filters);
            }

            using var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return $"ERROR: Failed to list time entries (HTTP {(int)response.StatusCode})";
            }

            string content = await response.Content.ReadAsStringAsync();
            return FormatJsonResponse(content, maxResults);
        }
        catch (Exception ex)
        {
            return $"ERROR: Failed to list time entries: {ex.Message}";
        }
    }

    [Description("Get time entries logged today.")]
    public async Task<string> GetTimeEntriesForToday()
    {
        return await ListTimeEntries(DateTime.Now.ToString("yyyy-MM-dd"), DateTime.Now.ToString("yyyy-MM-dd"), "", 100);
    }

    [Description("Get time entries logged this week (Monday to Sunday).")]
    public async Task<string> GetTimeEntriesForThisWeek()
    {
        var today = DateTime.Now;
        var startOfWeek = today.AddDays(-(int)today.DayOfWeek + 1);
        var endOfWeek = startOfWeek.AddDays(6);
        return await ListTimeEntries(startOfWeek.ToString("yyyy-MM-dd"), endOfWeek.ToString("yyyy-MM-dd"), "", 100);
    }

    [Description("Get a specific time entry by ID.")]
    public async Task<string> GetTimeEntryDetails(
        [Description("Time entry ID")] string entryId)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return "ERROR: Paymo API key not configured.";
        }

        try
        {
            using var response = await _httpClient.GetAsync($"{BaseUrl}/entries/{entryId}");
            if (!response.IsSuccessStatusCode)
            {
                return $"ERROR: Time entry not found (HTTP {(int)response.StatusCode})";
            }

            string content = await response.Content.ReadAsStringAsync();
            return FormatJsonResponse(content);
        }
        catch (Exception ex)
        {
            return $"ERROR: Failed to get time entry details: {ex.Message}";
        }
    }

    [Description("Get time entry summary by day for a date range.")]
    public async Task<string> GetDailySummary(
        [Description("Start date in YYYY-MM-DD format")] string startDate,
        [Description("End date in YYYY-MM-DD format")] string endDate)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return "ERROR: Paymo API key not configured.";
        }

        try
        {
            string url = $"{BaseUrl}/reports/entries?where=date>='{startDate}' AND date<='{endDate}'&group_by=date";
            using var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return $"ERROR: Failed to get daily summary (HTTP {(int)response.StatusCode})";
            }

            string content = await response.Content.ReadAsStringAsync();
            return FormatJsonResponse(content);
        }
        catch (Exception ex)
        {
            return $"ERROR: Failed to get daily summary: {ex.Message}";
        }
    }

    [Description("Get time entry summary by user for a date range.")]
    public async Task<string> GetUserSummary(
        [Description("Start date in YYYY-MM-DD format")] string startDate,
        [Description("End date in YYYY-MM-DD format")] string endDate)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return "ERROR: Paymo API key not configured.";
        }

        try
        {
            string url = $"{BaseUrl}/reports/entries?where=date>='{startDate}' AND date<='{endDate}'&group_by=user_id";
            using var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return $"ERROR: Failed to get user summary (HTTP {(int)response.StatusCode})";
            }

            string content = await response.Content.ReadAsStringAsync();
            return FormatJsonResponse(content);
        }
        catch (Exception ex)
        {
            return $"ERROR: Failed to get user summary: {ex.Message}";
        }
    }

    [Description("Get time entry summary by project for a date range.")]
    public async Task<string> GetProjectSummary(
        [Description("Start date in YYYY-MM-DD format")] string startDate,
        [Description("End date in YYYY-MM-DD format")] string endDate)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return "ERROR: Paymo API key not configured.";
        }

        try
        {
            string url = $"{BaseUrl}/reports/entries?where=date>='{startDate}' AND date<='{endDate}'&group_by=project_id";
            using var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return $"ERROR: Failed to get project summary (HTTP {(int)response.StatusCode})";
            }

            string content = await response.Content.ReadAsStringAsync();
            return FormatJsonResponse(content);
        }
        catch (Exception ex)
        {
            return $"ERROR: Failed to get project summary: {ex.Message}";
        }
    }

    [Description("Delete a time entry by ID.")]
    public async Task<string> DeleteTimeEntry(
        [Description("Time entry ID to delete")] string entryId)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return "ERROR: Paymo API key not configured.";
        }

        try
        {
            using var response = await _httpClient.DeleteAsync($"{BaseUrl}/entries/{entryId}");
            if (!response.IsSuccessStatusCode)
            {
                return $"ERROR: Failed to delete time entry (HTTP {(int)response.StatusCode})";
            }

            return $"✓ Time entry {entryId} deleted successfully";
        }
        catch (Exception ex)
        {
            return $"ERROR: Failed to delete time entry: {ex.Message}";
        }
    }

    [Description("Get current authenticated user's information.")]
    public async Task<string> GetCurrentUser()
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return "ERROR: Paymo API key not configured.";
        }

        try
        {
            using var response = await _httpClient.GetAsync($"{BaseUrl}/me");
            if (!response.IsSuccessStatusCode)
            {
                return $"ERROR: Failed to get current user (HTTP {(int)response.StatusCode})";
            }

            string content = await response.Content.ReadAsStringAsync();
            return FormatJsonResponse(content);
        }
        catch (Exception ex)
        {
            return $"ERROR: Failed to get current user: {ex.Message}";
        }
    }

    [Description("Get company/workspace information.")]
    public async Task<string> GetCompanyInfo()
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return "ERROR: Paymo API key not configured.";
        }

        try
        {
            using var response = await _httpClient.GetAsync($"{BaseUrl}/company");
            if (!response.IsSuccessStatusCode)
            {
                return $"ERROR: Failed to get company info (HTTP {(int)response.StatusCode})";
            }

            string content = await response.Content.ReadAsStringAsync();
            return FormatJsonResponse(content);
        }
        catch (Exception ex)
        {
            return $"ERROR: Failed to get company info: {ex.Message}";
        }
    }

    [Description("Log time to a Paymo task by name or ID, supporting both duration and time range input.")]
    public async Task<string> LogTimeByTaskName(
        [Description("Task name (e.g., 'Ticket 1234567: Fix login page') or numeric task ID")] string taskName = "",
        [Description("Start time in HH:MM format (e.g., '08:00')")] string startTime = "",
        [Description("End time in HH:MM format (e.g., '09:00')")] string endTime = "",
        [Description("Hours spent as number or decimal (e.g., '2' or '1.5')")] string hours = "",
        [Description("Notes about the work done")] string notes = "",
        [Description("Date in YYYY-MM-DD format; defaults to today")] string date = "")
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                return "ERROR: Paymo API key not configured. Set PAYMO_API_KEY environment variable.";
            }

            if (string.IsNullOrWhiteSpace(taskName))
            {
                return "ERROR: Task name or ID is required.";
            }

            ValidateTimeInput(startTime, endTime, hours);

            string? taskId = await ResolveTaskId(taskName);
            if (string.IsNullOrEmpty(taskId))
            {
                return $"ERROR: Could not find task '{taskName}'. Try using the numeric task ID instead.";
            }

            string finalDate = string.IsNullOrWhiteSpace(date) ? DateTime.Now.ToString("yyyy-MM-dd") : date;
            int durationSeconds = CalculateDuration(startTime, endTime, hours);
            if (durationSeconds <= 0)
            {
                return "ERROR: Duration must be greater than zero.";
            }

            var entry = new TimeEntryRequest
            {
                TaskId = int.Parse(taskId),
                Date = finalDate,
                Duration = durationSeconds,
                Description = notes ?? ""
            };

            using var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/entries", entry);

            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync();
                return $"ERROR: Failed to create time entry (HTTP {(int)response.StatusCode}). Response: {error}";
            }

            string hours_display = (durationSeconds / 3600).ToString();
            string mins_display = ((durationSeconds % 3600) / 60).ToString();

            var summary = new List<string>
            {
                "✓ Time entry created successfully",
                $"  Task: {taskName}",
                $"  Date: {finalDate}",
                $"  Duration: {hours_display} hours" + (mins_display != "0" ? $" {mins_display} minutes" : "")
            };

            if (!string.IsNullOrWhiteSpace(notes))
            {
                summary.Add($"  Notes: {notes}");
            }

            return string.Join(Environment.NewLine, summary);
        }
        catch (ArgumentException ex)
        {
            return $"ERROR: {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"ERROR: Unexpected error: {ex.Message}";
        }
    }

    private async Task<string?> ResolveTaskId(string taskInput)
    {
        taskInput = taskInput?.Trim() ?? "";

        if (string.IsNullOrEmpty(taskInput))
        {
            throw new ArgumentException("Task name or ID cannot be empty.");
        }

        if (int.TryParse(taskInput, NumberStyles.None, CultureInfo.InvariantCulture, out _))
        {
            return taskInput;
        }

        try
        {
            using var response = await _httpClient.GetAsync($"{BaseUrl}/tasks?where=name='{Uri.EscapeDataString(taskInput)}'");

            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                string? taskId = ExtractTaskId(content);
                if (!string.IsNullOrEmpty(taskId))
                {
                    return taskId;
                }
            }

            using var response2 = await _httpClient.GetAsync($"{BaseUrl}/tasks?name__contains={Uri.EscapeDataString(taskInput)}");

            if (response2.IsSuccessStatusCode)
            {
                string content = await response2.Content.ReadAsStringAsync();
                string? taskId = ExtractTaskId(content);
                if (!string.IsNullOrEmpty(taskId))
                {
                    return taskId;
                }
            }

            var ticketMatch = System.Text.RegularExpressions.Regex.Match(taskInput, @"\d{7}");
            if (ticketMatch.Success)
            {
                string ticketNum = ticketMatch.Value;
                using var response3 = await _httpClient.GetAsync($"{BaseUrl}/tasks?name__contains={ticketNum}");

                if (response3.IsSuccessStatusCode)
                {
                    string content = await response3.Content.ReadAsStringAsync();
                    string? taskId = ExtractTaskId(content);
                    if (!string.IsNullOrEmpty(taskId))
                    {
                        return taskId;
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to resolve task ID for '{taskInput}': {ex.Message}", ex);
        }
    }

    private static string? ExtractTaskId(string jsonResponse)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonResponse);

            if (doc.RootElement.TryGetProperty("tasks", out var tasksElement))
            {
                if (tasksElement.ValueKind == JsonValueKind.Array && tasksElement.GetArrayLength() > 0)
                {
                    var firstTask = tasksElement[0];
                    if (firstTask.TryGetProperty("id", out var idElement))
                    {
                        return idElement.GetInt32().ToString();
                    }
                }
            }

            if (doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0)
            {
                var firstTask = doc.RootElement[0];
                if (firstTask.TryGetProperty("id", out var idElement))
                {
                    return idElement.GetInt32().ToString();
                }
            }
        }
        catch
        {
        }

        return null;
    }

    internal static void ValidateTimeInput(string startTime, string endTime, string hours)
    {
        bool hasTimeRange = !string.IsNullOrWhiteSpace(startTime) && !string.IsNullOrWhiteSpace(endTime);
        bool hasHours = !string.IsNullOrWhiteSpace(hours);

        if (!hasTimeRange && !hasHours)
        {
            throw new ArgumentException("Must provide either start_time/end_time OR hours parameter.");
        }

        if (hasTimeRange)
        {
            if (!IsValidTimeFormat(startTime))
            {
                throw new ArgumentException($"Invalid start_time format: '{startTime}'. Use HH:MM (e.g., '08:00').");
            }

            if (!IsValidTimeFormat(endTime))
            {
                throw new ArgumentException($"Invalid end_time format: '{endTime}'. Use HH:MM (e.g., '09:00').");
            }
        }

        if (hasHours)
        {
            if (!decimal.TryParse(hours, NumberStyles.Number, CultureInfo.InvariantCulture, out var hoursValue) || hoursValue <= 0)
            {
                throw new ArgumentException($"Invalid hours value: '{hours}'. Use a positive number (e.g., '2' or '1.5').");
            }
        }
    }

    private static bool IsValidTimeFormat(string time)
    {
        if (string.IsNullOrWhiteSpace(time))
        {
            return false;
        }

        var parts = time.Split(':');
        if (parts.Length != 2)
        {
            return false;
        }

        return int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var hours) &&
               int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var minutes) &&
               hours >= 0 && hours < 24 &&
               minutes >= 0 && minutes < 60;
    }

    internal static int CalculateDuration(string startTime, string endTime, string hours)
    {
        if (!string.IsNullOrWhiteSpace(startTime) && !string.IsNullOrWhiteSpace(endTime))
        {
            var (startHours, startMins) = ParseTime(startTime);
            var (endHours, endMins) = ParseTime(endTime);

            int startSecs = startHours * 3600 + startMins * 60;
            int endSecs = endHours * 3600 + endMins * 60;

            if (endSecs < startSecs)
            {
                endSecs += 86400;
            }

            return endSecs - startSecs;
        }

        if (decimal.TryParse(hours, NumberStyles.Number, CultureInfo.InvariantCulture, out var hoursValue))
        {
            return (int)Math.Round(hoursValue * 3600, MidpointRounding.AwayFromZero);
        }

        return 0;
    }

    private static (int hours, int minutes) ParseTime(string time)
    {
        var parts = time.Split(':');
        int hours = int.Parse(parts[0]);
        int minutes = int.Parse(parts[1]);
        return (hours, minutes);
    }

    private static string FormatJsonResponse(string jsonContent, int? maxItems = null)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonContent);
            var options = new JsonSerializerOptions { WriteIndented = true };

            if (maxItems.HasValue && maxItems > 0)
            {
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("tasks", out var tasks) && tasks.ValueKind == JsonValueKind.Array)
                    {
                        var limited = LimitArrayItems(tasks, maxItems.Value);
                        return JsonSerializer.Serialize(limited, options);
                    }
                    if (root.TryGetProperty("projects", out var projects) && projects.ValueKind == JsonValueKind.Array)
                    {
                        var limited = LimitArrayItems(projects, maxItems.Value);
                        return JsonSerializer.Serialize(limited, options);
                    }
                    if (root.TryGetProperty("entries", out var entries) && entries.ValueKind == JsonValueKind.Array)
                    {
                        var limited = LimitArrayItems(entries, maxItems.Value);
                        return JsonSerializer.Serialize(limited, options);
                    }
                    if (root.TryGetProperty("users", out var users) && users.ValueKind == JsonValueKind.Array)
                    {
                        var limited = LimitArrayItems(users, maxItems.Value);
                        return JsonSerializer.Serialize(limited, options);
                    }
                    if (root.TryGetProperty("clients", out var clients) && clients.ValueKind == JsonValueKind.Array)
                    {
                        var limited = LimitArrayItems(clients, maxItems.Value);
                        return JsonSerializer.Serialize(limited, options);
                    }
                }
            }

            return JsonSerializer.Serialize(doc.RootElement, options);
        }
        catch
        {
            return jsonContent;
        }
    }

    private static JsonElement[] LimitArrayItems(JsonElement array, int limit)
    {
        var items = new List<JsonElement>();
        int count = 0;
        foreach (var item in array.EnumerateArray())
        {
            if (count >= limit) break;
            items.Add(item);
            count++;
        }
        return items.ToArray();
    }

    private sealed class TimeEntryRequest
    {
        [JsonPropertyName("task_id")]
        public int TaskId { get; set; }

        [JsonPropertyName("date")]
        public string Date { get; set; } = "";

        [JsonPropertyName("duration")]
        public int Duration { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; } = "";
    }
}
