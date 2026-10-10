using System.Net;
using System.Text.Json;
using louis_agent.core.config;
using louis_agent.core.usage;

namespace louis_agent.core.tests;

// PRICES_URL: the price table over HTTP (e.g. louis-agent.api's GET /prices), falling back to the file when unreachable.
public class PricesUrlTests
{
    private const string UrlTable = """
        { "as_of": "2026-10-11", "currency": "USD",
          "models": { "claude-haiku-5-5": { "input": 0.10, "output": 0.50, "cache_read": 0.01, "cache_write_5m": 0.125, "cache_write_1h": 0.20 } } }
        """;

    private const string FileTable = """
        { "as_of": "2026-10-10", "currency": "USD",
          "models": { "claude-haiku-5-5": { "input": 0.10, "output": 0.50, "cache_read": 0.01, "cache_write_5m": 0.125, "cache_write_1h": 0.20 } } }
        """;

    private string _dir = null!;
    private string _file = null!;

    [SetUp]
    public void SetUp()
    {
        _dir = Directory.CreateTempSubdirectory("prices-url-").FullName;
        _file = Path.Combine(_dir, "prices.json");
        File.WriteAllText(_file, FileTable);
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_dir, recursive: true);

    private AgentOptions Options(string url) => new() { PricesUrl = url, PricesFile = _file };

    private static (PriceTable Prices, string Stderr) Load(AgentOptions options, HttpMessageHandler handler)
    {
        var stderr = new StringWriter();
        var original = Console.Error;
        Console.SetError(stderr);
        try
        {
            return (AgentHost.LoadPrices(options, handler), stderr.ToString());
        }
        finally
        {
            Console.SetError(original);
        }
    }

    [Test]
    public void LoadPrices_UrlAnswers_UsesItsTable()
    {
        Uri? asked = null;
        var handler = new StubHandler(request =>
        {
            asked = request.RequestUri;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(UrlTable) };
        });

        var (prices, _) = Load(Options("http://demo-api:8080/prices"), handler);

        Assert.That(asked, Is.EqualTo(new Uri("http://demo-api:8080/prices")));
        Assert.That(prices.AsOf, Is.EqualTo("2026-10-11"), "the URL's table, not the file's");
    }

    [Test]
    public void LoadPrices_UrlAnswersWithAnError_FallsBackToTheFile()
    {
        // louis-agent.api answers 404 when it has no table of its own.
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        var (prices, stderr) = Load(Options("http://demo-api:8080/prices"), handler);

        Assert.That(prices.AsOf, Is.EqualTo("2026-10-10"));
        Assert.That(stderr, Does.Contain("[WARN] Prices: http://demo-api:8080/prices can't be reached"));
    }

    [Test]
    public void LoadPrices_UrlUnreachable_FallsBackToTheFile()
    {
        var handler = new StubHandler(_ => throw new HttpRequestException("Connection refused"));

        var (prices, stderr) = Load(Options("http://demo-api:8080/prices"), handler);

        Assert.That(prices.AsOf, Is.EqualTo("2026-10-10"));
        Assert.That(stderr, Does.Contain("Connection refused"));
    }

    [Test]
    public void LoadPrices_UrlAnswersSomethingThatIsNotATable_Throws()
    {
        // Reached, but wrong: like a malformed file, the host doesn't start on it.
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("<html>") });

        var ex = Assert.Throws<InvalidDataException>(() => AgentHost.LoadPrices(Options("http://demo-api:8080/prices"), handler));
        Assert.That(ex!.Message, Does.Contain("http://demo-api:8080/prices"));
    }

    [TestCase("demo-api:8080/prices")]
    [TestCase("file:///etc/prices.json")]
    public void LoadPrices_NotAnHttpUrl_Throws(string url) =>
        Assert.Throws<InvalidDataException>(() => AgentHost.LoadPrices(Options(url), new StubHandler(_ => throw new InvalidOperationException())));

    [Test]
    public void PricesUrlSetting_IsReadFromTheEnvironment()
    {
        var options = AgentOptions.FromEnvironment(name => name == "PRICES_URL" ? "http://demo-api:8080/prices" : null);

        Assert.That(options.PricesUrl, Is.EqualTo("http://demo-api:8080/prices"));
    }

    [Test]
    public void PriceTable_WrittenAsJson_ReadsBackTheSame()
    {
        // GET /prices serves the API's table as JSON; another host must read back exactly the same prices.
        PriceTable shipped = PriceTable.Load(Path.Combine(TestPaths.RepoRoot, "config", "prices.json"));

        PriceTable copy = PriceTable.Parse(JsonSerializer.Serialize(shipped, PriceTable.Json), "GET /prices");

        Assert.That(copy.AsOf, Is.EqualTo(shipped.AsOf));
        Assert.That(copy.Models.Keys, Is.EquivalentTo(shipped.Models.Keys));
        Assert.That(copy.Find("claude-haiku-5-5")!.LongPrompt!.Output, Is.EqualTo(shipped.Find("claude-haiku-5-5")!.LongPrompt!.Output));
        Assert.That(copy.Find("claude-opus-5-5")!.CacheWrite1h, Is.EqualTo(shipped.Find("claude-opus-5-5")!.CacheWrite1h));
    }
}