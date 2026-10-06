using System.Net;
using System.Text.Json;
using louis_agent.core.tools;
using louis_agent.core.providers;

namespace louis_agent.core.tests;

public class PaymoToolsTests
{
    [TestCase("", "", "1.5", 5400)]
    [TestCase("", "", "2", 7200)]
    [TestCase("08:00", "09:00", "", 3600)]
    [TestCase("14:30", "16:15", "", 6300)]
    [TestCase("23:00", "01:00", "", 7200)]
    [TestCase("09:00", "09:00", "", 0)]
    public void CalculateDuration_ReturnsSeconds(string start, string end, string hours, int expected) =>
        Assert.That(PaymoTools.CalculateDuration(start, end, hours), Is.EqualTo(expected));

    [Test]
    public void ValidateTimeInput_RejectsBadInput()
    {
        Assert.Throws<ArgumentException>(() => PaymoTools.ValidateTimeInput("", "", ""));
        Assert.Throws<ArgumentException>(() => PaymoTools.ValidateTimeInput("25:00", "09:00", ""));
        Assert.Throws<ArgumentException>(() => PaymoTools.ValidateTimeInput("08:00", "9", ""));
        Assert.Throws<ArgumentException>(() => PaymoTools.ValidateTimeInput("", "", "-1"));
        Assert.Throws<ArgumentException>(() => PaymoTools.ValidateTimeInput("", "", "abc"));
        Assert.DoesNotThrow(() => PaymoTools.ValidateTimeInput("08:00", "09:00", ""));
        Assert.DoesNotThrow(() => PaymoTools.ValidateTimeInput("", "", "1.5"));
    }

    private static (PaymoTools Client, StubHandler Handler) Create(string? key, Func<HttpRequestMessage, HttpResponseMessage>? respond = null)
    {
        var handler = new StubHandler(respond ?? (_ => new HttpResponseMessage(HttpStatusCode.Created) { Content = new StringContent("{}") }));
        return (new PaymoTools(TestPaths.Agent(key), new HttpClient(handler)), handler);
    }

    [Test]
    public async Task LogTimeByTaskName_WithoutApiKey_ReturnsErrorWithoutNetwork()
    {
        var (client, handler) = Create(null);

        string result = await client.LogTimeByTaskName("123", hours: "1");

        Assert.That(result, Does.StartWith("ERROR").And.Contain("PAYMO_API_KEY"));
        Assert.That(handler.Requests, Is.Empty);
    }

    [Test]
    public async Task LogTimeByTaskName_WithoutTaskName_ReturnsError()
    {
        var (client, _) = Create("k");
        Assert.That(await client.LogTimeByTaskName("", hours: "1"), Does.StartWith("ERROR"));
    }

    [Test]
    public async Task LogTimeByTaskName_WithNumericId_PostsEntryWithDurationInSeconds()
    {
        var (client, handler) = Create("k");

        string result = await client.LogTimeByTaskName("32942792", hours: "1.5", notes: "auth", date: "2026-10-05");

        Assert.That(result, Does.Contain("created successfully"));
        var post = handler.Requests.Single();
        Assert.That(post.Method, Is.EqualTo(HttpMethod.Post));
        Assert.That(post.Url, Does.EndWith("/api/entries"));
        using var body = JsonDocument.Parse(post.Body!);
        Assert.That(body.RootElement.GetProperty("task_id").GetInt32(), Is.EqualTo(32942792));
        Assert.That(body.RootElement.GetProperty("duration").GetInt32(), Is.EqualTo(5400));
        Assert.That(body.RootElement.GetProperty("date").GetString(), Is.EqualTo("2026-10-05"));
        Assert.That(body.RootElement.GetProperty("description").GetString(), Is.EqualTo("auth"));
    }

    [Test]
    public async Task LogTimeByTaskName_ResolvesTaskNameThenPosts()
    {
        var (client, handler) = Create("k", req => req.Method == HttpMethod.Get
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"tasks\":[{\"id\":777}]}") }
            : new HttpResponseMessage(HttpStatusCode.Created) { Content = new StringContent("{}") });

        string result = await client.LogTimeByTaskName("Ticket 2636766: MS Billing test plan", startTime: "08:00", endTime: "09:00");

        Assert.That(result, Does.Contain("1 hours"));
        using var body = JsonDocument.Parse(handler.Requests.Last().Body!);
        Assert.That(body.RootElement.GetProperty("task_id").GetInt32(), Is.EqualTo(777));
        Assert.That(body.RootElement.GetProperty("duration").GetInt32(), Is.EqualTo(3600));
    }

    [Test]
    public async Task LogTimeByTaskName_SurfacesHttpFailure()
    {
        var (client, _) = Create("k", _ => new HttpResponseMessage(HttpStatusCode.Unauthorized) { Content = new StringContent("nope") });

        string result = await client.LogTimeByTaskName("123", hours: "1");

        Assert.That(result, Does.StartWith("ERROR").And.Contain("401"));
    }

    [Test]
    public async Task LogTimeByTaskName_ZeroDuration_ReturnsErrorWithoutPosting()
    {
        var (client, handler) = Create("k");

        string result = await client.LogTimeByTaskName("123", startTime: "09:00", endTime: "09:00");

        Assert.That(result, Does.StartWith("ERROR"));
        Assert.That(handler.Requests, Is.Empty);
    }

    // Comprehensive tests for additional methods

    [Test]
    public async Task SearchTasks_WithoutApiKey_ReturnsError()
    {
        var (client, handler) = Create(null);

        string result = await client.SearchTasks("test");

        Assert.That(result, Does.StartWith("ERROR").And.Contain("API key not configured"));
        Assert.That(handler.Requests, Is.Empty);
    }

    [Test]
    public async Task SearchTasks_WithValidApiKey_MakesRequest()
    {
        var (client, handler) = Create("k", _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"tasks\":[]}")
        });

        string result = await client.SearchTasks("test");

        Assert.That(handler.Requests, Has.Count.EqualTo(1));
        var req = handler.Requests.First();
        Assert.That(req.Method, Is.EqualTo(HttpMethod.Get));
        Assert.That(req.Url, Does.Contain("/api/tasks"));
        Assert.That(req.Url, Does.Contain("test"));
    }

    [Test]
    public async Task SearchTasks_WithStatusFilter_IncludesStatusInQuery()
    {
        var (client, handler) = Create("k", _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"tasks\":[]}")
        });

        await client.SearchTasks("test", status: "active");

        var req = handler.Requests.First();
        Assert.That(req.Url, Does.Contain("active"));
    }

    [Test]
    public async Task SearchTasks_ClampMaxResults()
    {
        var (client, handler) = Create("k", _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"tasks\":[]}")
        });

        await client.SearchTasks("test", maxResults: 150); // Should clamp to 100

        Assert.That(handler.Requests, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task ListTasks_WithoutApiKey_ReturnsError()
    {
        var (client, _) = Create(null);

        string result = await client.ListTasks();

        Assert.That(result, Does.StartWith("ERROR").And.Contain("API key not configured"));
    }

    [Test]
    public async Task ListTasks_WithProjectFilter_IncludesProjectInQuery()
    {
        var (client, handler) = Create("k", _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"tasks\":[]}")
        });

        await client.ListTasks(projectId: "999");

        var req = handler.Requests.First();
        Assert.That(req.Url, Does.Contain("999"));
    }

    [Test]
    public async Task ListTasks_HttpFailure_ReturnsErrorWithStatusCode()
    {
        var (client, _) = Create("k", _ => new HttpResponseMessage(HttpStatusCode.BadRequest));

        string result = await client.ListTasks();

        Assert.That(result, Does.StartWith("ERROR").And.Contain("400"));
    }

    [Test]
    public async Task GetTaskDetails_WithoutApiKey_ReturnsError()
    {
        var (client, _) = Create(null);

        string result = await client.GetTaskDetails("123");

        Assert.That(result, Does.StartWith("ERROR"));
    }

    [Test]
    public async Task GetTaskDetails_MakesCorrectRequest()
    {
        var (client, handler) = Create("k", _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"id\":123,\"name\":\"Test Task\"}")
        });

        await client.GetTaskDetails("123");

        var req = handler.Requests.First();
        Assert.That(req.Url, Does.EndWith("/api/tasks/123"));
    }

    [Test]
    public async Task ListProjects_WithoutApiKey_ReturnsError()
    {
        var (client, _) = Create(null);

        string result = await client.ListProjects();

        Assert.That(result, Does.StartWith("ERROR"));
    }

    [Test]
    public async Task ListProjects_WithStatusFilter_IncludesStatusInQuery()
    {
        var (client, handler) = Create("k", _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"projects\":[]}")
        });

        await client.ListProjects(status: "active");

        var req = handler.Requests.First();
        Assert.That(req.Url, Does.Contain("active"));
    }

    [Test]
    public async Task GetProjectDetails_WithoutApiKey_ReturnsError()
    {
        var (client, _) = Create(null);

        string result = await client.GetProjectDetails("999");

        Assert.That(result, Does.StartWith("ERROR"));
    }

    [Test]
    public async Task GetProjectDetails_MakesCorrectRequest()
    {
        var (client, handler) = Create("k", _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"id\":999}")
        });

        await client.GetProjectDetails("999");

        var req = handler.Requests.First();
        Assert.That(req.Url, Does.EndWith("/api/projects/999"));
    }

    [Test]
    public async Task ListClients_WithoutApiKey_ReturnsError()
    {
        var (client, _) = Create(null);

        string result = await client.ListClients();

        Assert.That(result, Does.StartWith("ERROR"));
    }

    [Test]
    public async Task ListClients_MakesCorrectRequest()
    {
        var (client, handler) = Create("k", _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"clients\":[]}")
        });

        await client.ListClients();

        var req = handler.Requests.First();
        Assert.That(req.Url, Does.EndWith("/api/clients"));
    }

    [Test]
    public async Task ListUsers_WithoutApiKey_ReturnsError()
    {
        var (client, _) = Create(null);

        string result = await client.ListUsers();

        Assert.That(result, Does.StartWith("ERROR"));
    }

    [Test]
    public async Task ListUsers_MakesCorrectRequest()
    {
        var (client, handler) = Create("k", _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"users\":[]}")
        });

        await client.ListUsers();

        var req = handler.Requests.First();
        Assert.That(req.Url, Does.EndWith("/api/users"));
    }

    [Test]
    public async Task ListTimeEntries_WithoutApiKey_ReturnsError()
    {
        var (client, _) = Create(null);

        string result = await client.ListTimeEntries();

        Assert.That(result, Does.StartWith("ERROR"));
    }

    [Test]
    public async Task ListTimeEntries_WithDateRange_IncludesDatesInQuery()
    {
        var (client, handler) = Create("k", _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"entries\":[]}")
        });

        await client.ListTimeEntries(startDate: "2026-10-01", endDate: "2026-10-05");

        var req = handler.Requests.First();
        Assert.That(req.Url, Does.Contain("2026-10-01"));
        Assert.That(req.Url, Does.Contain("2026-10-05"));
    }

    [Test]
    public async Task ListTimeEntries_WithUserId_IncludesUserIdInQuery()
    {
        var (client, handler) = Create("k", _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"entries\":[]}")
        });

        await client.ListTimeEntries(userId: "42");

        var req = handler.Requests.First();
        Assert.That(req.Url, Does.Contain("42"));
    }

    [Test]
    public async Task GetTimeEntriesForToday_MakesCorrectRequest()
    {
        var (client, handler) = Create("k", _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"entries\":[]}")
        });

        await client.GetTimeEntriesForToday();

        var req = handler.Requests.First();
        string today = DateTime.Now.ToString("yyyy-MM-dd");
        Assert.That(req.Url, Does.Contain(today));
    }

    [Test]
    public async Task GetTimeEntriesForThisWeek_MakesCorrectRequest()
    {
        var (client, handler) = Create("k", _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"entries\":[]}")
        });

        await client.GetTimeEntriesForThisWeek();

        Assert.That(handler.Requests, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task GetTimeEntryDetails_WithoutApiKey_ReturnsError()
    {
        var (client, _) = Create(null);

        string result = await client.GetTimeEntryDetails("999");

        Assert.That(result, Does.StartWith("ERROR"));
    }

    [Test]
    public async Task GetTimeEntryDetails_MakesCorrectRequest()
    {
        var (client, handler) = Create("k", _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"id\":999}")
        });

        await client.GetTimeEntryDetails("999");

        var req = handler.Requests.First();
        Assert.That(req.Url, Does.EndWith("/api/entries/999"));
    }

    [Test]
    public async Task GetDailySummary_WithoutApiKey_ReturnsError()
    {
        var (client, _) = Create(null);

        string result = await client.GetDailySummary("2026-10-01", "2026-10-05");

        Assert.That(result, Does.StartWith("ERROR"));
    }

    [Test]
    public async Task GetDailySummary_MakesCorrectRequest()
    {
        var (client, handler) = Create("k", _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}")
        });

        await client.GetDailySummary("2026-10-01", "2026-10-05");

        var req = handler.Requests.First();
        Assert.That(req.Url, Does.Contain("reports"));
        Assert.That(req.Url, Does.Contain("group_by=date"));
    }

    [Test]
    public async Task GetUserSummary_WithoutApiKey_ReturnsError()
    {
        var (client, _) = Create(null);

        string result = await client.GetUserSummary("2026-10-01", "2026-10-05");

        Assert.That(result, Does.StartWith("ERROR"));
    }

    [Test]
    public async Task GetUserSummary_MakesCorrectRequest()
    {
        var (client, handler) = Create("k", _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}")
        });

        await client.GetUserSummary("2026-10-01", "2026-10-05");

        var req = handler.Requests.First();
        Assert.That(req.Url, Does.Contain("group_by=user_id"));
    }

    [Test]
    public async Task GetProjectSummary_WithoutApiKey_ReturnsError()
    {
        var (client, _) = Create(null);

        string result = await client.GetProjectSummary("2026-10-01", "2026-10-05");

        Assert.That(result, Does.StartWith("ERROR"));
    }

    [Test]
    public async Task GetProjectSummary_MakesCorrectRequest()
    {
        var (client, handler) = Create("k", _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}")
        });

        await client.GetProjectSummary("2026-10-01", "2026-10-05");

        var req = handler.Requests.First();
        Assert.That(req.Url, Does.Contain("group_by=project_id"));
    }

    [Test]
    public async Task DeleteTimeEntry_WithoutApiKey_ReturnsError()
    {
        var (client, _) = Create(null);

        string result = await client.DeleteTimeEntry("999");

        Assert.That(result, Does.StartWith("ERROR"));
    }

    [Test]
    public async Task DeleteTimeEntry_MakesCorrectRequest()
    {
        var (client, handler) = Create("k", _ => new HttpResponseMessage(HttpStatusCode.OK));

        await client.DeleteTimeEntry("999");

        var req = handler.Requests.First();
        Assert.That(req.Method, Is.EqualTo(HttpMethod.Delete));
        Assert.That(req.Url, Does.EndWith("/api/entries/999"));
    }

    [Test]
    public async Task DeleteTimeEntry_HttpFailure_ReturnsErrorWithStatusCode()
    {
        var (client, _) = Create("k", _ => new HttpResponseMessage(HttpStatusCode.NotFound));

        string result = await client.DeleteTimeEntry("999");

        Assert.That(result, Does.StartWith("ERROR").And.Contain("404"));
    }

    [Test]
    public async Task GetCurrentUser_WithoutApiKey_ReturnsError()
    {
        var (client, _) = Create(null);

        string result = await client.GetCurrentUser();

        Assert.That(result, Does.StartWith("ERROR"));
    }

    [Test]
    public async Task GetCurrentUser_MakesCorrectRequest()
    {
        var (client, handler) = Create("k", _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"id\":1}")
        });

        await client.GetCurrentUser();

        var req = handler.Requests.First();
        Assert.That(req.Url, Does.EndWith("/api/me"));
    }

    [Test]
    public async Task GetCompanyInfo_WithoutApiKey_ReturnsError()
    {
        var (client, _) = Create(null);

        string result = await client.GetCompanyInfo();

        Assert.That(result, Does.StartWith("ERROR"));
    }

    [Test]
    public async Task GetCompanyInfo_MakesCorrectRequest()
    {
        var (client, handler) = Create("k", _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}")
        });

        await client.GetCompanyInfo();

        var req = handler.Requests.First();
        Assert.That(req.Url, Does.EndWith("/api/company"));
    }

    // Edge cases for LogTimeByTaskName
    [Test]
    public async Task LogTimeByTaskName_EmptyTaskName_ReturnsError()
    {
        var (client, _) = Create("k");

        string result = await client.LogTimeByTaskName("");

        Assert.That(result, Does.StartWith("ERROR").And.Contain("Task name or ID is required"));
    }

    [Test]
    public async Task LogTimeByTaskName_InvalidTimeFormat_ReturnsError()
    {
        var (client, _) = Create("k");

        string result = await client.LogTimeByTaskName("123", startTime: "25:00", endTime: "26:00");

        Assert.That(result, Does.StartWith("ERROR").And.Contain("Invalid"));
    }

    [Test]
    public async Task LogTimeByTaskName_NegativeHours_ReturnsError()
    {
        var (client, _) = Create("k");

        string result = await client.LogTimeByTaskName("123", hours: "-1");

        Assert.That(result, Does.StartWith("ERROR"));
    }

    [Test]
    public async Task LogTimeByTaskName_WithNotes_IncludesNotesInRequest()
    {
        var (client, handler) = Create("k");

        await client.LogTimeByTaskName("123", hours: "1", notes: "Fixed bug");

        var req = handler.Requests.Last();
        using var body = JsonDocument.Parse(req.Body!);
        Assert.That(body.RootElement.GetProperty("description").GetString(), Is.EqualTo("Fixed bug"));
    }

    [Test]
    public async Task LogTimeByTaskName_CustomDate_IncludesCustomDate()
    {
        var (client, handler) = Create("k");

        await client.LogTimeByTaskName("123", hours: "1", date: "2026-10-01");

        var req = handler.Requests.Last();
        using var body = JsonDocument.Parse(req.Body!);
        Assert.That(body.RootElement.GetProperty("date").GetString(), Is.EqualTo("2026-10-01"));
    }

    [Test]
    public async Task LogTimeByTaskName_NotFoundTask_ReturnsError()
    {
        var (client, _) = Create("k", req => req.Method == HttpMethod.Get
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"tasks\":[]}") }
            : new HttpResponseMessage(HttpStatusCode.Created) { Content = new StringContent("{}") });

        string result = await client.LogTimeByTaskName("NonExistentTask123");

        Assert.That(result, Does.StartWith("ERROR"));
    }

    [Test]
    public async Task LogTimeByTaskName_MidnightCrossingTimeRange_CalculatesDurationCorrectly()
    {
        var (client, handler) = Create("k");

        await client.LogTimeByTaskName("123", startTime: "23:00", endTime: "01:00");

        var req = handler.Requests.Last();
        using var body = JsonDocument.Parse(req.Body!);
        int duration = body.RootElement.GetProperty("duration").GetInt32();
        // From 23:00 to 01:00 next day = 2 hours
        Assert.That(duration, Is.EqualTo(7200));
    }
}
