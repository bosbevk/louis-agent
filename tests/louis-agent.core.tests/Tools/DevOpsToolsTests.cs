using System.Net;
using System.Text.Json;
using louis_agent.core.tools;

namespace louis_agent.core.tests;

public class DevOpsToolsTests
{
    private static (DevOpsTools Tools, StubHandler Handler) Create(
        string? key = null, Func<HttpRequestMessage, HttpResponseMessage>? respond = null, string? team = null)
    {
        var handler = new StubHandler(respond ?? (_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"workItems\":[]}")
        }));
        var httpClient = new HttpClient(handler);
        var devopsKey = key ?? "test-key";
        var devops = new DevOpsTools(devopsKey, "contoso", "Contoso Web", team);

        // Use reflection to set the private HttpClient field
        var field = typeof(DevOpsTools).GetField("_httpClient",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(devops, httpClient);

        return (devops, handler);
    }

    [Test]
    public async Task QueryWorkItems_WithoutApiKey_ReturnsError()
    {
        var devops = new DevOpsTools("");

        string result = await devops.QueryWorkItems("SELECT * FROM workitems");

        Assert.That(result, Does.Contain("Error").And.Contain("DEVOPS_API_KEY"));
    }

    [Test]
    public async Task QueryWorkItems_WithValidKey_MakesPostRequest()
    {
        var (devops, handler) = Create("key");

        await devops.QueryWorkItems("SELECT [System.Id] FROM workitems");

        Assert.That(handler.Requests, Has.Count.EqualTo(1));
        var req = handler.Requests.First();
        Assert.That(req.Method, Is.EqualTo(HttpMethod.Post));
        Assert.That(req.Url, Does.Contain("/wit/wiql"));
    }

    [Test]
    public async Task QueryWorkItems_IncludesQueryInBody()
    {
        var (devops, handler) = Create("key");

        await devops.QueryWorkItems("SELECT [System.Id] FROM workitems WHERE [System.State]='Active'");

        var req = handler.Requests.First();
        using var doc = JsonDocument.Parse(req.Body!);
        Assert.That(doc.RootElement.GetProperty("query").GetString(), Does.Contain("Active"));
    }

    [Test]
    public async Task GetWorkItemsByAssignee_WithoutApiKey_ReturnsError()
    {
        var devops = new DevOpsTools("");

        string result = await devops.GetWorkItemsByAssignee();

        Assert.That(result, Does.Contain("Error"));
    }

    [Test]
    public async Task GetWorkItemsByAssignee_MakesCorrectQuery()
    {
        var (devops, handler) = Create("key");

        await devops.GetWorkItemsByAssignee("Sprint 42", "Jane Doe");

        Assert.That(handler.Requests, Has.Count.EqualTo(1));
        var req = handler.Requests.First();
        Assert.That(req.Body, Does.Contain("Sprint 42"));
        Assert.That(req.Body, Does.Contain("Jane Doe"));
        Assert.That(req.Body, Does.Contain("AssignedTo"));
    }

    [Test]
    public async Task GetUserStoriesBySprint_WithoutApiKey_ReturnsError()
    {
        var devops = new DevOpsTools("");

        string result = await devops.GetUserStoriesBySprint();

        Assert.That(result, Does.Contain("Error"));
    }

    [Test]
    public async Task GetUserStoriesBySprint_FiltersUserStories()
    {
        var (devops, handler) = Create("key");

        await devops.GetUserStoriesBySprint("Sprint 42");

        var req = handler.Requests.First();
        Assert.That(req.Body, Does.Contain("User Story"));
    }

    [Test]
    public async Task GetTasksBySprint_WithoutApiKey_ReturnsError()
    {
        var devops = new DevOpsTools("");

        string result = await devops.GetTasksBySprint();

        Assert.That(result, Does.Contain("Error"));
    }

    [Test]
    public async Task GetTasksBySprint_FiltersTaskType()
    {
        var (devops, handler) = Create("key");

        await devops.GetTasksBySprint("Sprint 42");

        var req = handler.Requests.First();
        Assert.That(req.Body, Does.Contain("Task"));
    }

    [Test]
    public async Task GetBugsBySprint_WithoutApiKey_ReturnsError()
    {
        var devops = new DevOpsTools("");

        string result = await devops.GetBugsBySprint();

        Assert.That(result, Does.Contain("Error"));
    }

    [Test]
    public async Task GetBugsBySprint_FiltersBugType()
    {
        var (devops, handler) = Create("key");

        await devops.GetBugsBySprint("Sprint 42");

        var req = handler.Requests.First();
        Assert.That(req.Body, Does.Contain("Bug"));
    }

    [Test]
    public async Task GetWorkItemById_WithoutApiKey_ReturnsError()
    {
        var devops = new DevOpsTools("");

        string result = await devops.GetWorkItemById("123");

        Assert.That(result, Does.Contain("Error"));
    }

    [Test]
    public async Task GetWorkItemById_MakesGetRequest()
    {
        var (devops, handler) = Create("key", _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"id\":123,\"fields\":{\"System.Title\":\"Test Item\"}}")
        });

        await devops.GetWorkItemById("123");

        var req = handler.Requests.First();
        Assert.That(req.Method, Is.EqualTo(HttpMethod.Get));
        Assert.That(req.Url, Does.Contain("/wit/workitems/123"));
    }

    [Test]
    public async Task GetWorkItemById_HttpFailure_ReturnsError()
    {
        var (devops, _) = Create("key", _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("Work item not found")
        });

        string result = await devops.GetWorkItemById("999");

        Assert.That(result, Does.Contain("Error"));
    }

    [Test]
    public async Task GetWorkItemsByState_WithoutApiKey_ReturnsError()
    {
        var devops = new DevOpsTools("");

        string result = await devops.GetWorkItemsByState("Active");

        Assert.That(result, Does.Contain("Error"));
    }

    [Test]
    public async Task GetWorkItemsByState_FiltersByState()
    {
        var (devops, handler) = Create("key");

        await devops.GetWorkItemsByState("Active", "Sprint 42");

        var req = handler.Requests.First();
        Assert.That(req.Body, Does.Contain("Active"));
    }

    [Test]
    public async Task FindWorkItemsByTitle_WithoutApiKey_ReturnsError()
    {
        var devops = new DevOpsTools("");

        string result = await devops.FindWorkItemsByTitle("bug");

        Assert.That(result, Does.Contain("Error"));
    }

    [Test]
    public async Task FindWorkItemsByTitle_FiltersByKeyword()
    {
        var (devops, handler) = Create("key");

        await devops.FindWorkItemsByTitle("authentication", "Sprint 42");

        var req = handler.Requests.First();
        Assert.That(req.Body, Does.Contain("authentication"));
    }

    [Test]
    public async Task FindWorkItemsByTitle_ClampMaxResults()
    {
        var (devops, handler) = Create("key");

        await devops.FindWorkItemsByTitle("test", "Sprint 42", maxResults: 150);

        Assert.That(handler.Requests, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task GetOpenIssuesBySprint_WithoutApiKey_ReturnsError()
    {
        var devops = new DevOpsTools("");

        string result = await devops.GetOpenIssuesBySprint();

        Assert.That(result, Does.Contain("Error"));
    }

    [Test]
    public async Task GetOpenIssuesBySprint_FiltersOpenStates()
    {
        var (devops, handler) = Create("key");

        await devops.GetOpenIssuesBySprint("Sprint 42");

        var req = handler.Requests.First();
        Assert.That(req.Body, Does.Contain("Active").Or.Contain("New"));
    }

    [Test]
    public async Task GetUnassignedWorkItems_WithoutApiKey_ReturnsError()
    {
        var devops = new DevOpsTools("");

        string result = await devops.GetUnassignedWorkItems();

        Assert.That(result, Does.Contain("Error"));
    }

    [Test]
    public async Task GetUnassignedWorkItems_FiltersUnassigned()
    {
        var (devops, handler) = Create("key");

        await devops.GetUnassignedWorkItems("Sprint 42");

        var req = handler.Requests.First();
        Assert.That(req.Body, Does.Contain("AssignedTo"));
    }

    [Test]
    public async Task GetBlockersBySprint_WithoutApiKey_ReturnsError()
    {
        var devops = new DevOpsTools("");

        string result = await devops.GetBlockersBySprint();

        Assert.That(result, Does.Contain("Error"));
    }

    [Test]
    public async Task GetBlockersBySprint_FiltersBlockerType()
    {
        var (devops, handler) = Create("key");

        await devops.GetBlockersBySprint("Sprint 42");

        var req = handler.Requests.First();
        Assert.That(req.Body, Does.Contain("Blocker"));
    }

    [Test]
    public async Task GetRecentlyModifiedWorkItems_WithoutApiKey_ReturnsError()
    {
        var devops = new DevOpsTools("");

        string result = await devops.GetRecentlyModifiedWorkItems();

        Assert.That(result, Does.Contain("Error"));
    }

    [Test]
    public async Task GetRecentlyModifiedWorkItems_CalculatesDateCorrectly()
    {
        var (devops, handler) = Create("key");

        await devops.GetRecentlyModifiedWorkItems(days: 7, sprint: "Sprint 42");

        var req = handler.Requests.First();
        string cutoffDate = DateTime.Now.AddDays(-7).ToString("yyyy-MM-dd");
        // Body should contain the cutoff date or a reference to changed date filtering
        Assert.That(req.Body, Does.Contain("ChangedDate").Or.Contain(cutoffDate));
    }

    [Test]
    public async Task GetRecentlyModifiedWorkItems_DefaultDays()
    {
        var (devops, handler) = Create("key");

        await devops.GetRecentlyModifiedWorkItems(sprint: "Sprint 42");

        Assert.That(handler.Requests, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task GetSprintSummary_WithoutApiKey_ReturnsError()
    {
        var devops = new DevOpsTools("");

        string result = await devops.GetSprintSummary();

        Assert.That(result, Does.Contain("Error"));
    }

    [Test]
    public async Task GetSprintSummary_MakesCorrectQuery()
    {
        var (devops, handler) = Create("key");

        await devops.GetSprintSummary("Sprint 42");

        var req = handler.Requests.First();
        Assert.That(req.Body, Does.Contain("Sprint 42"));
    }

    [Test]
    public async Task GetAvailableSprints_WithoutApiKey_ReturnsError()
    {
        var devops = new DevOpsTools("");

        string result = await devops.GetAvailableSprints();

        Assert.That(result, Does.Contain("Error"));
    }

    [Test]
    public async Task GetAvailableSprints_MakesGetRequest()
    {
        var (devops, handler) = Create("key", _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"value\":[{\"name\":\"Sprint 41\"},{\"name\":\"Sprint 42\"}]}")
        });

        string result = await devops.GetAvailableSprints();

        var req = handler.Requests.First();
        Assert.That(req.Method, Is.EqualTo(HttpMethod.Get));
        Assert.That(req.Url, Does.Contain("/work/teamsettings/iterations"));
        Assert.That(result, Does.Contain("Sprint 41").Or.Contain("Sprint 42"));
    }

    [Test]
    public async Task GetAvailableSprints_HttpFailure_ReturnsError()
    {
        var (devops, _) = Create("key", _ => new HttpResponseMessage(HttpStatusCode.BadRequest));

        string result = await devops.GetAvailableSprints();

        Assert.That(result, Does.Contain("Error"));
    }

    [Test]
    public async Task GetTeamWorkloadBySprint_WithoutApiKey_ReturnsError()
    {
        var devops = new DevOpsTools("");

        string result = await devops.GetTeamWorkloadBySprint();

        Assert.That(result, Does.Contain("Error"));
    }

    [Test]
    public async Task GetTeamWorkloadBySprint_MakesCorrectQuery()
    {
        var (devops, handler) = Create("key");

        await devops.GetTeamWorkloadBySprint("Sprint 42");

        var req = handler.Requests.First();
        Assert.That(req.Body, Does.Contain("Sprint 42"));
    }

    [Test]
    public async Task CreateWorkItem_WithoutApiKey_ReturnsError()
    {
        var devops = new DevOpsTools("");

        string result = await devops.CreateWorkItem("Task", "New Task");

        Assert.That(result, Does.Contain("Error"));
    }

    [Test]
    public async Task CreateWorkItem_MakesPostRequest()
    {
        var (devops, handler) = Create("key", _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"id\":123,\"fields\":{\"System.Title\":\"New Task\"}}")
        });

        string result = await devops.CreateWorkItem("Task", "New Task");

        var req = handler.Requests.First();
        Assert.That(req.Method, Is.EqualTo(HttpMethod.Post));
        Assert.That(req.Url, Does.Contain("/wit/workitems/$Task"));
    }

    [Test]
    public async Task CreateWorkItem_IncludesTitleInBody()
    {
        var (devops, handler) = Create("key", _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"id\":123}")
        });

        await devops.CreateWorkItem("Task", "Fix authentication bug");

        var req = handler.Requests.First();
        Assert.That(req.Body, Does.Contain("Fix authentication bug"));
    }

    [Test]
    public async Task CreateWorkItem_WithDescription_IncludesDescriptionInBody()
    {
        var (devops, handler) = Create("key", _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"id\":123}")
        });

        await devops.CreateWorkItem("Task", "Fix bug", description: "This is a detailed description");

        var req = handler.Requests.First();
        Assert.That(req.Body, Does.Contain("This is a detailed description"));
    }

    [Test]
    public async Task CreateWorkItem_HttpFailure_ReturnsError()
    {
        var (devops, _) = Create("key", _ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("Invalid work item type")
        });

        string result = await devops.CreateWorkItem("InvalidType", "Test");

        Assert.That(result, Does.Contain("Error"));
    }

    [Test]
    public async Task UpdateWorkItem_WithoutApiKey_ReturnsError()
    {
        var devops = new DevOpsTools("");

        string result = await devops.UpdateWorkItem("123", "System.State", "Active");

        Assert.That(result, Does.Contain("Error"));
    }

    [Test]
    public async Task UpdateWorkItem_MakesPatchRequest()
    {
        var (devops, handler) = Create("key", _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"id\":123}")
        });

        await devops.UpdateWorkItem("123", "System.State", "Active");

        var req = handler.Requests.First();
        Assert.That(req.Method.Method, Is.EqualTo("PATCH"));
        Assert.That(req.Url, Does.Contain("/wit/workitems/123"));
    }

    [Test]
    public async Task UpdateWorkItem_IncludesFieldAndValueInBody()
    {
        var (devops, handler) = Create("key", _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"id\":123}")
        });

        await devops.UpdateWorkItem("123", "System.State", "Resolved");

        var req = handler.Requests.First();
        Assert.That(req.Body, Does.Contain("System.State"));
        Assert.That(req.Body, Does.Contain("Resolved"));
    }

    [Test]
    public async Task UpdateWorkItem_HttpFailure_ReturnsError()
    {
        var (devops, _) = Create("key", _ => new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = new StringContent("Conflict updating work item")
        });

        string result = await devops.UpdateWorkItem("123", "System.State", "Invalid");

        Assert.That(result, Does.Contain("Error"));
    }

    // Edge cases and error handling

    [Test]
    public async Task QueryWorkItems_EmptyQuery_MakesRequest()
    {
        var (devops, handler) = Create("key");

        await devops.QueryWorkItems("");

        Assert.That(handler.Requests, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task GetWorkItemsByAssignee_CustomSprint()
    {
        var (devops, handler) = Create("key");

        await devops.GetWorkItemsByAssignee(sprint: "Sprint 200", assignee: "John Doe");

        var req = handler.Requests.First();
        Assert.That(req.Body, Does.Contain("Sprint 200"));
        Assert.That(req.Body, Does.Contain("John Doe"));
    }

    [Test]
    public async Task FindWorkItemsByTitle_EmptyKeyword_MakesRequest()
    {
        var (devops, handler) = Create("key");

        await devops.FindWorkItemsByTitle("", "Sprint 42");

        Assert.That(handler.Requests, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task GetRecentlyModifiedWorkItems_ZeroDays()
    {
        var (devops, handler) = Create("key");

        await devops.GetRecentlyModifiedWorkItems(days: 0);

        Assert.That(handler.Requests, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task CreateWorkItem_WithSpecialCharactersInTitle()
    {
        var (devops, handler) = Create("key", _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"id\":123}")
        });

        await devops.CreateWorkItem("Task", "Fix bug & \"critical\" issue");

        var req = handler.Requests.First();
        Assert.That(req.Body, Is.Not.Empty);
    }

    [Test]
    public async Task UpdateWorkItem_WithEmptyFieldValue()
    {
        var (devops, handler) = Create("key", _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"id\":123}")
        });

        await devops.UpdateWorkItem("123", "System.Description", "");

        var req = handler.Requests.First();
        Assert.That(req.Body, Does.Contain("System.Description"));
    }

    // The WIQL text of a request (the body is JSON, which escapes quotes and backslashes).
    private static string Query(string? body) => JsonDocument.Parse(body!).RootElement.GetProperty("query").GetString()!;

    [Test]
    public async Task SprintTools_UseTheConfiguredOrganisationAndProject()
    {
        var (devops, handler) = Create("key");

        await devops.GetTasksBySprint("Sprint 42");

        var req = handler.Requests.Single();
        Assert.That(req.Url, Does.StartWith("https://dev.azure.com/contoso/Contoso Web/_apis/wit/wiql"));
        Assert.That(Query(req.Body), Does.Contain("[System.TeamProject]='Contoso Web'"));
        Assert.That(Query(req.Body), Does.Contain(@"Under 'Contoso Web\\Sprint 42'"));
    }

    [Test]
    public async Task SprintTools_WithATeam_LookInTheTeamsIterations()
    {
        var (devops, handler) = Create("key", team: "Platform");

        await devops.GetTasksBySprint("Sprint 42");

        Assert.That(Query(handler.Requests.Single().Body), Does.Contain(@"Under 'Contoso Web\\Platform\\Sprint 42'"));
    }

    [Test]
    public async Task SprintTools_WithoutASprint_UseTheTeamsCurrentSprint()
    {
        const string currentSprint = """{"value":[{"name":"Sprint 43","path":"Contoso Web\\Platform\\Sprint 43"}]}""";
        var (devops, handler) = Create("key", req => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(req.RequestUri!.Query.Contains("timeframe=current") ? currentSprint : """{"workItems":[]}""")
        }, team: "Platform");

        string result = await devops.GetBugsBySprint();

        Assert.That(handler.Requests, Has.Count.EqualTo(2), result);
        Assert.That(handler.Requests[0].Url, Does.Contain("/contoso/Contoso Web/Platform/_apis/work/teamsettings/iterations"));
        Assert.That(Query(handler.Requests[1].Body), Does.Contain(@"Under 'Contoso Web\\Platform\\Sprint 43'"));
    }

    [Test]
    public async Task SprintTools_WhenThereIsNoCurrentSprint_AskForOne()
    {
        var (devops, handler) = Create("key", _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("""{"value":[]}""") });

        string result = await devops.GetBugsBySprint();

        Assert.That(result, Does.Contain("no current sprint").And.Contain("GetAvailableSprints"));
        Assert.That(handler.Requests, Has.Count.EqualTo(1), "no work-item query without a sprint");
    }

    [Test]
    public async Task GetWorkItemsByAssignee_WithoutAnAssignee_MeansTheKeysOwner()
    {
        var (devops, handler) = Create("key");

        await devops.GetWorkItemsByAssignee("Sprint 42");

        Assert.That(Query(handler.Requests.Single().Body), Does.Contain("[System.AssignedTo] = @Me"));
    }
}
