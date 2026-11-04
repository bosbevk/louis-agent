using System.Globalization;
using System.Net;
using System.Text.Json.Nodes;
using System.Web;
using LouisAgent.Packs.WhereToBuy.Branches.Map;

namespace LouisAgent.Packs.WhereToBuy.Tests.Shops;

/// <summary>
/// The map as the shop lookups meet it: the three real answers saved from OpenStreetMap on 2026-11-04 (trimmed to what
/// the code reads), the three towns they were searched around, and a fake map that answers a box the way Nominatim
/// does, with only the places inside it and at most twenty.
/// </summary>
internal static class ShopMaps
{
    /// <summary>Malmesbury as the map has it.</summary>
    internal const string Malmesbury = """
        [{"name":"Malmesbury","category":"boundary","type":"administrative","lat":"-33.460278","lon":"18.723611",
          "address":{"town":"Malmesbury","municipality":"Swartland Local Municipality","state":"Western Cape"}}]
        """;

    /// <summary>Prince Albert as the map has it (the centre is approximate, for these tests).</summary>
    internal const string PrinceAlbert = """
        [{"name":"Prince Albert","category":"boundary","type":"administrative","lat":"-33.2186","lon":"22.0259",
          "address":{"town":"Prince Albert","municipality":"Prince Albert Local Municipality",
                     "state":"Western Cape"}}]
        """;

    /// <summary>Sea Point as the map has it, a suburb of Cape Town (the centre is approximate here).</summary>
    internal const string SeaPoint = """
        [{"name":"Sea Point","category":"place","type":"suburb","lat":"-33.9186","lon":"18.3846",
          "address":{"suburb":"Sea Point","city":"Cape Town","state":"Western Cape"}}]
        """;

    /// <summary>One of the saved answers, by file name.</summary>
    internal static string Fixture(string name) => File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "tests",
        "louis-agent.pack.where-to-buy.tests", "Shops", "Fixtures", name));

    /// <summary>
    /// A map that answers the town lookup with the town, and every box with the places of the answer that lie inside
    /// it, the first twenty in the answer's order; it records what was asked.
    /// </summary>
    internal static (MapSearch Map, List<Uri> Asked) Around(string town, string places,
        HttpStatusCode status = HttpStatusCode.OK)
    {
        var asked = new List<Uri>();
        var http = new HttpClient(new StubHandler(request =>
        {
            asked.Add(request.RequestUri!);
            string body = request.RequestUri!.Query.Contains("limit=1&") ? town : Inside(request.RequestUri, places);
            return new HttpResponseMessage(status) { Content = new StringContent(body) };
        }));
        return (new MapSearch(http, TimeSpan.Zero), asked);
    }

    /// <summary>The places of an answer that lie inside the request's viewbox, twenty at most.</summary>
    private static string Inside(Uri url, string places)
    {
        string[] box = HttpUtility.ParseQueryString(url.Query)["viewbox"]!.Split(',');
        double west = Number(box[0]), north = Number(box[1]), east = Number(box[2]), south = Number(box[3]);
        var inside = JsonNode.Parse(places)!.AsArray().OfType<JsonNode>().Where(place =>
            Number(place["lon"]!.GetValue<string>()) >= west && Number(place["lon"]!.GetValue<string>()) <= east &&
            Number(place["lat"]!.GetValue<string>()) <= north && Number(place["lat"]!.GetValue<string>()) >= south);
        return new JsonArray([.. inside.Take(20).Select(place => place.DeepClone())]).ToJsonString();
    }

    /// <summary>A number as the map writes it.</summary>
    private static double Number(string text) => double.Parse(text, CultureInfo.InvariantCulture);
}
