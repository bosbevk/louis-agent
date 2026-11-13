using LouisAgent.Choices;

namespace LouisAgent.Diagrams.Icons;

/// <summary>
/// The icons a card may wear, by the names a diagram's file uses: a set of Tabler Icons (MIT) for what a project's
/// parts usually are, and since F127.4.1 the facts a ticket shows (a date, a time, a phone, a place, a price, a pet),
/// each named for what it shows, and since F134.1 what a showroom's demos wear (a bed, a meal, scales, an exchange)
/// and a deck's two arrows, when the set became the one a page's line icons are drawn from too. <see cref="IconPaths"/> draws them.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public enum DiagramIcon
{
    [WireName("chip")] Chip,
    [WireName("user")] User,
    [WireName("users")] Users,
    [WireName("box")] Box,
    [WireName("server")] Server,
    [WireName("database")] Database,
    [WireName("world")] World,
    [WireName("message")] Message,
    [WireName("shield")] Shield,
    [WireName("code")] Code,
    [WireName("folder")] Folder,
    [WireName("file")] File,
    [WireName("browser")] Browser,
    [WireName("checks")] Checks,
    [WireName("chart")] Chart,
    [WireName("cloud")] Cloud,
    [WireName("settings")] Settings,
    [WireName("branch")] Branch,
    [WireName("mail")] Mail,
    [WireName("search")] Search,
    [WireName("book")] Book,
    [WireName("terminal")] Terminal,
    [WireName("stack")] Stack,
    [WireName("info")] Info,
    [WireName("calendar")] Calendar,
    [WireName("clock")] Clock,
    [WireName("phone")] Phone,
    [WireName("home")] Home,
    [WireName("tag")] Tag,
    [WireName("paw")] Paw,
    [WireName("bed")] Bed,
    [WireName("dining")] Dining,
    [WireName("scales")] Scales,
    [WireName("exchange")] Exchange,
    [WireName("chevron-left")] ChevronLeft,
    [WireName("chevron-right")] ChevronRight
}
