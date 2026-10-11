using System.Net;
using LouisAgent.Core.Config;
using louis_agent.core.usage;
using louis_agent.core;

namespace LouisAgent.Core.Tests;

// F14 step 1: RATES_URL (Frankfurter by default, or another louis-agent's GET /rates), mapped by RATES_FORMAT, falling back
// to RATES_FILE when it can't be used; like PRICES_URL and the price file.
public class RatesUrlTests
{
    private const string FrankfurterReply =
        """{"amount":1.0,"base":"USD","date":"2026-10-11","rates":{"ZAR":16.6}}""";

    private const string FileRates = """{ "as_of": "2026-10-09", "base": "USD", "source": "file", "rates": { "ZAR": 16.5331 } }""";

    private string _dir = null!;
    private string _file = null!;

    [SetUp]
    public void SetUp()
    {
        _dir = Directory.CreateTempSubdirectory("rates-url-").FullName;
        _file = Path.Combine(_dir, "rates.json");
        File.WriteAllText(_file, FileRates);
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_dir, recursive: true);

    private AgentOptions Options(string? url, string format = "frankfurter") => new() { RatesUrl = url, RatesFormat = format, RatesFile = _file };

    private static StubHandler Answer(HttpStatusCode status, string body) =>
        new(_ => new HttpResponseMessage(status) { Content = new StringContent(body) });

    private static (ExchangeRates Rates, string Stderr) Load(AgentOptions options, HttpMessageHandler? handler = null)
    {
        var stderr = new StringWriter();
        var original = Console.Error;
        Console.SetError(stderr);
        try
        {
            return (AgentHost.LoadRates(options, "USD", handler), stderr.ToString());
        }
        finally
        {
            Console.SetError(original);
        }
    }

    [Test]
    public void LoadRates_FrankfurterAnswers_UsesItsRates()
    {
        var handler = Answer(HttpStatusCode.OK, FrankfurterReply);

        var (rates, stderr) = Load(Options(AgentOptions.DefaultRatesUrl), handler);

        Assert.That(handler.Requests.Single().Url, Is.EqualTo(AgentOptions.DefaultRatesUrl));
        Assert.That((rates.AsOf, rates.RateFor("ZAR")), Is.EqualTo(("2026-10-11", (decimal?)16.6m)));
        Assert.That(stderr, Does.Contain("[INFO] Rates: https://api.frankfurter.dev"));
    }

    [Test]
    public void LoadRates_AnotherLouisAgentsRates_ReadAsOurs()
    {
        // The demo's orchestrator reads demo-api's GET /rates, which is already in our format.
        var handler = Answer(HttpStatusCode.OK, FileRates.Replace("\"file\"", "\"api\""));

        var (rates, _) = Load(Options("http://demo-api:8080/rates", RatesJsonMapper.FormatName), handler);

        Assert.That(rates.Source, Is.EqualTo("api"));
    }

    [Test]
    public void LoadRates_Unreachable_FallsBackToTheFile()
    {
        var handler = new StubHandler(_ => throw new HttpRequestException("No such host is known."));

        var (rates, stderr) = Load(Options(AgentOptions.DefaultRatesUrl), handler);

        Assert.That(rates.Source, Is.EqualTo("file"));
        Assert.That(stderr, Does.Contain("[WARN] Rates:").And.Contain("using the rates file instead"));
    }

    [TestCase(HttpStatusCode.InternalServerError, "{}")]
    [TestCase(HttpStatusCode.OK, "<html>maintenance</html>")]
    [TestCase(HttpStatusCode.OK, """{"error":"not found"}""")]
    public void LoadRates_ErrorOrAReplyItCantMap_FallsBackToTheFile(HttpStatusCode status, string body)
    {
        // A third party's reply changing shape, or failing, never stops an agent.
        var (rates, stderr) = Load(Options(AgentOptions.DefaultRatesUrl), Answer(status, body));

        Assert.That(rates.Source, Is.EqualTo("file"));
        Assert.That(stderr, Does.Contain("[WARN] Rates:"));
    }

    [Test]
    public void LoadRates_Off_ReadsTheFileWithoutAsking()
    {
        var handler = Answer(HttpStatusCode.OK, FrankfurterReply);

        var (rates, _) = Load(Options(url: null), handler);

        Assert.That(handler.Requests, Is.Empty);
        Assert.That(rates.Source, Is.EqualTo("file"));
    }

    [Test]
    public void LoadRates_UrlAnswersInAnotherCurrency_FallsBackToTheFile()
    {
        // The provider's change, not the operator's mistake (e.g. Frankfurter v2 reading ?from= as a date and answering in
        // EUR): warn and use the file, never stop every host from starting. Converting with EUR rates would be wrong.
        const string fromEur = """{"amount":1.0,"base":"EUR","date":"2026-10-09","rates":{"ZAR":18.53}}""";

        var (rates, stderr) = Load(Options(AgentOptions.DefaultRatesUrl), Answer(HttpStatusCode.OK, fromEur));

        Assert.That((rates.Source, rates.Base), Is.EqualTo(("file", "USD")));
        Assert.That(stderr, Does.Contain("[WARN] Rates:").And.Contain("are from EUR").And.Contain("using the rates file instead"));
    }

    [Test]
    public void DefaultRatesUrl_UsesV1sDocumentedBaseParameter()
    {
        Assert.That(AgentOptions.DefaultRatesUrl, Is.EqualTo("https://api.frankfurter.dev/v1/latest?base=USD"));
    }

    [Test]
    public void LoadRates_FileFromAnotherCurrency_StopsStartUp()
    {
        File.WriteAllText(_file, FileRates.Replace("\"USD\"", "\"EUR\""));

        Assert.That(() => Load(Options(url: null)), Throws.TypeOf<AgentHost.WrongBaseException>());
    }

    [Test]
    public void LoadRates_MalformedFile_StopsStartUpNamingIt()
    {
        File.WriteAllText(_file, "{ not json");

        Assert.That(() => Load(Options(url: null)), Throws.TypeOf<InvalidDataException>().With.Message.Contain(_file));
    }

    [Test]
    public void LoadRates_NoFile_IsEmptyWithAWarning()
    {
        File.Delete(_file);

        var (rates, stderr) = Load(Options(url: null));

        Assert.That(rates.IsEmpty, Is.True);
        Assert.That(stderr, Does.Contain("[WARN] No exchange rates").And.Contain("shown in USD"));
    }

    [Test]
    public void LoadRates_NotAnHttpUrl_Throws()
    {
        Assert.That(() => Load(Options("file:///etc/passwd")), Throws.TypeOf<InvalidDataException>().With.Message.Contain("RATES_URL"));
    }

    [TestCase(null, AgentOptions.DefaultRatesUrl)]
    [TestCase("off", null)]
    [TestCase("OFF", null)]
    [TestCase("http://demo-api:8080/rates", "http://demo-api:8080/rates")]
    public void RatesUrlSetting_DefaultsToFrankfurter_OffDisablesIt(string? value, string? expected)
    {
        var options = AgentOptions.FromEnvironment(name => name == "RATES_URL" ? value : null);

        Assert.That(options.RatesUrl, Is.EqualTo(expected));
    }

    [Test]
    public void RatesFormatAndFileSettings_AreRead()
    {
        var options = AgentOptions.FromEnvironment(name => name switch
        {
            "RATES_FORMAT" => "rates-json",
            "RATES_FILE" => "/config/rates.json",
            _ => null,
        });

        Assert.That((options.RatesFormat, options.RatesFile), Is.EqualTo(("rates-json", "/config/rates.json")));
    }
}