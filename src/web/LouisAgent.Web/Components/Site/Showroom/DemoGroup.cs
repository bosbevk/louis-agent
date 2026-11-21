using LouisAgent.Choices;

namespace LouisAgent.Web.Components.Site.Showroom;

/// <summary>
/// What a visitor comes to a demo for (F127.1): to see what an agent would do for their business, or to see agents
/// doing work. The showroom's rail groups its demos by it, in this order.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum DemoGroup
{
    [WireName("business")] ForYourBusiness,
    [WireName("at-work")] AgentsAtWork,
}
