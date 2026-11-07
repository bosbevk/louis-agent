# F59 The framework's web

> **Plan, for your review. Replaces the earlier versions, which were called "Pack web controls".** `web.shared` becomes
> `louis-agent.web`, the framework's web: everything that would be the same on any site. A site is a small program on top of it,
> and a pack's screens work one way. All twelve decisions are in. No code until you say go.
> **Elsewhere:** the split it extends in [ADR 0074](../adr/0074-the-folders-say-what-ships.md); the seam it follows in
> [ADR 0083](../adr/0083-packs-keep-reports-behind-the-admin-key.md); what gets amended in
> [ADR 0082](../../../packs/docs/adr/0082-the-recipe-library-is-an-open-feed.md).

## The goal, in Louis's words

"why don't packs have their own project, where the web controls live?", "should be overrideable and customised", "also add a
recipe management screen, which is driven from the pack", "all our pack controls will work like that", "move the puls layout to
shared and do the same as with admin, then we can spawn new sites, make a rule", and "the shared library becomes a framework web".

## In one paragraph

`louis-agent.web` holds what every site shares: the admin and chat frames with empty slots, the working parts, the generic pack
screens, and a base stylesheet. A site holds only what makes it that site: its words, brand, colours, addresses, landing page and
any pages of its own. A pack describes a screen as plain data, and a generic screen draws it. When a description can't say what's
wanted, a real component replaces it by name, and a demo's beats a pack's. It's the split the engine already has, now on the web.

## The rule, for your wording

> **A web file that would be the same on every site belongs in `louis-agent.web`. A site holds only its words, its brand and colours,
> its addresses, its own pages, and which packs and views it uses. The framework names no site.**

**Where it lives:** a new ADR, one line in `src/docs/CLAUDE.md`, and the how in `.claude/rules/razor.md`.

**What holds it,** so it isn't only words: a test that `louis-agent.web` has no site's name in its code, styles or scripts, and a
test that a site's layouts stay small, about 40 lines each.

## What others do, and where we differ

| Approach | Who does it | How it works |
|---|---|---|
| **The look and frame are a library the app picks** | [ABP themes](https://abp.io/docs/latest/framework/ui/blazor/theming) | A theme is a Razor Class Library holding layouts and global styles. Modules don't depend on a theme, and the app can override parts |
| **The plugin ships real pages** | [Microsoft](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/class-libraries?view=aspnetcore-10.0), [Backstage](https://backstage.io/docs/frontend-system/architecture/extensions), [Grafana](https://grafana.com/developers/plugin-tools/key-concepts/plugin-types-usage) | The plugin holds the pages, and the app adds them to its router and menu |
| **The plugin describes the screen** | [Django admin](https://docs.djangoproject.com/en/5.2/ref/contrib/admin/), [Filament](https://filamentphp.com/docs/4.x/resources/overview) | The plugin lists columns, search, actions and forms; one generic screen draws them; custom templates or pages cover the rest |
| **A new project comes from a template** | [`dotnet new`, Microsoft](https://learn.microsoft.com/en-us/dotnet/core/tools/templates) | An ordinary project plus a small config file becomes a template |
| **The formal spec: MCP Apps** | [MCP](https://modelcontextprotocol.io/docs/extensions/apps) | The server ships HTML that runs in a sandboxed frame inside a chat, and it acts only through the server's tools |

**Where we go against them:** a screen's description crosses the wire as JSON, whereas Django and Filament draw on the server. I
found no strong source on doing this for an admin. The risk is the vocabulary growing without end, so **a new kind of column, field
or control enters only when a second screen needs it; until then it's a custom component.** The edit form is the one exception,
which you chose knowingly, and it stays small: text, number, link and rows.

## Context: what we have today

- **Puls's web app is about 13,100 lines and `web.shared` about 2,200,** so most of the sorting is still to do.
- **Puls's `MainLayout` is only `@Body`.** The real chat frame is `ChatPage` and `Components/Chat`, about 1,500 lines. So the chat
  frame moves as a component, not a layout.
- **The chat's addresses are words:** `/gesels` and `/verken` in Puls, `/chat/{id}` in the code-review app. So a chat page stays a
  thin page in each site, whereas the admin's pages can be shared.
- **Puls's admin is about 10,600 lines, not the 4,000 I told you.** It calls only the framework API's `/v1/admin` routes. "Puls" appears
  in it mostly as page titles and the `pulsUi` script's name, so it looks movable.
- **The code-review app has its own chat page,** so it's a second real user of the chat frame, which tests the slots honestly.
- **`web.shared` has no stylesheet and no routable pages.** Puls's chat, frame and admin styles are about 1,600 lines. Its landing's
  341 lines stay.
- **The code-review app already has the colour contract we need.** Its `base/page.css` uses only variables that a theme sets (`--bg`,
  `--panel`, `--accent` and so on), with two themes, `retro` by default and `paper`. Puls has its own names (`--orange`, `--ink`).
- **Step 1 of the first plan is committed** (`AdminGate`) and holds. **Step 2 is uncommitted** (`ReportsPanel`), and goes under your
  pick on reports.
- **Recipes have no screen.** `IRecipeStore` can't remove or change a recipe, and `IExcludedPages` can't list or clear pages. The first
  copy of a page stays (`KeepAsync` only inserts), so a later read can't overwrite a hand edit.

## Decided by Louis

| # | Decision | Your pick | What it means here |
|---|---|---|---|
| | The overall shape | **C:** described first, a real component when needed | Packs' screens |
| | The name | **A:** `louis-agent.web` | The code-review app becomes `louis-agent.code-review`, with Rider's rename |
| 1 | Do reports become described screens? | **A:** yes, one seam | `IDataReport`, its route, `ReportCard` and the Reports page go, and ADR 0083 is replaced. Uncommitted step 2 is thrown away |
| 2 | Where does the generic page live? | **C:** in the framework, with the frame | A site writes no admin page |
| 3 | What draws the table? | **A:** a plain table of our own | No package |
| 4 | Removing a recipe | **A:** delete it and exclude the page | It stays gone until cleared. The answer says it may serve for five more minutes |
| 5 | What does the recipe screen do? | **C:** a full edit form | Forms enter the vocabulary. The address, the site and when it was read stay read-only |
| 6 | How do slots reach a shared frame? | **C:** a shared component, with a small layout in each site | `AdminShell` takes ordinary slots, checked at build. A site writes about fifteen lines |
| 7 | Editing lists with sections | **B:** a row per line | Each ingredient or step has a section box and buttons to add, remove and move. The vocabulary gains `Rows` |
| 8 | Does a hand edit say so? | **A:** mark it | The copy records when it was edited, and the screen shows it |
| 9 | How does a framework page get the site's admin frame? | **A:** a framework layout that hands over (Bridge) | Framework pages say `@layout AdminFrame`, which reads the site's layout from one setting and renders it with [`LayoutView`](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/layouts). A forgotten setting says so plainly |
| 10 | Where does the look live? | **A:** a base stylesheet in the framework, in louis-agent's colours | Styles move to `_content/louis-agent.web/`. With no colours of its own, a site looks like louis-agent. Puls sets its own |
| 11 | Do Puls's eval pages move too? | **A:** all of them, as their own child | F59.8, whose plan starts by proving nothing in them is Puls's own |
| 12 | How is a new site spawned? | **A:** a starter site plus a `dotnet new` template | F59.9. It works inside this repo until NuGet packages exist |

## Design: what changes

**A site says everything in a few small places.** Puls's admin layout becomes:

```razor
@inherits LayoutComponentBase

<AdminShell BackHref="gesels" BackLabel="@PulsText.BackToPuls" BrandTitle="@PulsText.Home">
    <Brand><PulsLogo /></Brand>
    <Top><LiveRunStrip /></Top>
    <ChildContent>@Body</ChildContent>
</AdminShell>
```

and `Program.cs` names the rest. The labels go through the site's message enum (ADR 0009), and are plain here to read easily:

```csharp
builder.Services.AddSingleton(new SiteFrames(Admin: typeof(AdminLayout)));
builder.Services.AddSingleton(PulsText.ChatWords);                     // the chat's words; with none, English defaults

var views = new PackViews();                                           // the default: ScreenTable for every screen
RecipeWebViews.AddTo(views);                                           // the pack's own, from packs-web
views.UseScreen<OurOwnLibraryView>("recipe-library");                  // a demo's override, if it wants one: the last wins
builder.Services.AddSingleton(views);
```

**The sidebar.** `AdminShell` draws the framework's admin links (once they've moved) and `PackMenu`, which asks the API what screens
exist, so a pack's link appears with no change to the site. A site can add its own links through a slot.

**The chat frame.** `ChatShell` holds the page's frame, state and handlers, with slots for what differs: the brand, the empty chat,
extra views such as Verken, and side panels. Each site keeps a thin page with its own addresses. Words come from a `ChatWords` record
with English defaults, and Puls fills it in Afrikaans.

**Pack screens.** One seam, `IDataScreen`, one family of routes, one generic screen, and one override list. A custom view can wrap the
default: `ScreenTable` takes an optional `Above` fragment, so the recipes view adds its *Of 10 offered, 1 served* line in about 20
lines. A row comes back as fields, each with a kind and whether it's read-only or required. The pack checks what's saved, and a
refusal names the field.

**The look.** The framework's base styles use only louis-agent's colour contract, and its default theme is louis-agent's `retro`, with
`paper` beside it. A site with no colours of its own looks like louis-agent. Puls sets the same variables to its orange and charcoal,
so it looks as it does today.

**Uploads keep their own seam.** A file, a preview and a save make a process, not a table. They share the frame, the sidebar,
`AdminGate` and the override list.

**Safety.** Everything sits behind the admin key. A screen can do only what its pack named, an unknown action or row is a 404, values
travel as data, and every save and action is logged. An action that destroys something carries a `Confirm` text and takes two clicks,
never a browser dialog. Nothing here is a tool, so no model can call it.

### Interface design

```mermaid
classDiagram
    class SiteFrames {
        <<F59.2, the setting>>
        +Type Admin
    }
    class AdminFrame {
        <<F59.2, hands over>>
        LayoutView of SiteFrames.Admin
    }
    class AdminShell {
        <<F59.2, the frame>>
        +RenderFragment Brand
        +RenderFragment Top
        +RenderFragment Links
        +RenderFragment ChildContent
        +string BackHref
    }
    class PulsAdminLayout {
        <<changed F59.2, Puls>>
        about 15 lines
    }
    class ChatShell {
        <<F59.3, the frame>>
        +RenderFragment Brand
        +RenderFragment Empty
        +RenderFragment Views
        +RenderFragment Panels
        +string SessionId
    }
    class ChatWords {
        <<F59.3, record>>
        English defaults
    }
    class PulsChatPage {
        <<changed F59.3, Puls>>
        its addresses and slots
    }
    class CodeReviewChatPage {
        <<changed F59.3, code-review>>
        its addresses and slots
    }
    class IDataScreen {
        <<F59.5, grows in F59.6>>
        +ScreenSpec Spec
        +ReadAsync(ScreenQuery, CancellationToken) ScreenPage
        +ReadRowAsync(string, CancellationToken) ScreenRecord
        +SaveRowAsync(string, ScreenValues, CancellationToken) ScreenAnswer
        +ActAsync(string, string, CancellationToken) ScreenAnswer
    }
    class RecipesUsageScreen {
        <<F59.5>>
    }
    class RecipesScreen {
        <<F59.6>>
    }
    class ExcludedPagesScreen {
        <<F59.6>>
    }
    class PackViews {
        <<F59.5>>
        +UseScreen~TView~(string) PackViews
        +UseImport~TView~(string) PackViews
        +ScreenView(string) Type
        +ImportView(string) Type
    }
    class IScreenView {
        <<F59.5>>
        +ScreenReply Screen
    }
    class IImportView {
        <<F59.7>>
        +ImportForm Form
    }
    class ScreenTable {
        <<F59.5, the default>>
        +ScreenReply Screen
        +RenderFragment Above
    }
    class ScreenForm {
        <<F59.6>>
        +ScreenReply Screen
        +string RowId
    }
    class PackMenu {
        <<F59.5>>
        a sidebar link per screen
    }
    class RecipeLibraryView {
        <<F59.5, packs-web>>
        +ScreenReply Screen
    }
    class ImportCard {
        <<moved F59.7>>
        +ImportForm Form
    }
    AdminFrame --> SiteFrames : reads
    AdminFrame ..> PulsAdminLayout : renders
    PulsAdminLayout --> AdminShell : fills
    AdminShell --> PackMenu : shows
    PulsChatPage --> ChatShell : fills
    CodeReviewChatPage --> ChatShell : fills
    ChatShell --> ChatWords : reads
    IDataScreen <|.. RecipesUsageScreen
    IDataScreen <|.. RecipesScreen
    IDataScreen <|.. ExcludedPagesScreen
    IScreenView <|.. ScreenTable
    IScreenView <|.. RecipeLibraryView
    IImportView <|.. ImportCard
    RecipeLibraryView --> ScreenTable : wraps
    ScreenTable --> ScreenForm : opens a row
    ScreenTable --> PackViews : chosen by
```

```csharp
// src/web/louis-agent.web: the frames. F59.2, F59.3
public sealed record SiteFrames(Type Admin);        // the site's admin layout, read by AdminFrame
public sealed record ChatWords { /* every word the chat frame shows, each with an English default */ }

// src/engine/louis-agent.composition/Packs/Screens: what a pack implements. Plain data, no Blazor. F59.5
public interface IDataScreen
{
    ScreenSpec Spec { get; }
    Task<ScreenPage> ReadAsync(ScreenQuery query, CancellationToken cancellationToken);   // an empty page if its store is down
    // F59.6, each refusing by default, so a read-only screen writes none of them
    Task<ScreenRecord?> ReadRowAsync(string rowId, CancellationToken cancellationToken);
    Task<ScreenAnswer> SaveRowAsync(string rowId, ScreenValues values, CancellationToken cancellationToken);
    Task<ScreenAnswer> ActAsync(string action, string? rowId, CancellationToken cancellationToken);
}

public sealed record ScreenSpec(string Name, string Title, string Description, IReadOnlyList<ScreenColumn> Columns,
    bool Searchable, IReadOnlyList<ScreenAction> Actions);
public sealed record ScreenColumn(string Title, ScreenCell Kind);
public enum ScreenCell { Text, Number, Link, Date }                         // the whole vocabulary for a column
public sealed record ScreenQuery(string? Search, int Page);
public sealed record ScreenPage(IReadOnlyList<ScreenRow> Rows, long Total);
public sealed record ScreenRow(string Id, IReadOnlyList<string?> Cells);    // null is "unknown", never 0 or ""

// F59.6: what can be done, and one row as a form
public sealed record ScreenAction(string Name, string Label, ScreenActionOn On, string? Confirm);
public enum ScreenActionOn { Row, Screen }
public sealed record ScreenRecord(string Title, IReadOnlyList<ScreenField> Fields);
public sealed record ScreenField(string Name, string Label, ScreenFieldKind Kind, bool ReadOnly, bool Required,
    string? Value, IReadOnlyList<string> RowColumns, IReadOnlyList<IReadOnlyList<string>> Rows);
public enum ScreenFieldKind { Text, Number, Link, Rows }                    // the whole vocabulary for a field
public sealed record ScreenValues(IReadOnlyDictionary<string, string?> Values,
    IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyList<string>>> Rows);
public sealed record ScreenAnswer(bool Done, string Message, IReadOnlyDictionary<string, string> FieldErrors);

// src/web/louis-agent.web/Components/Packs: the last one registered wins. F59.5
public interface IScreenView { ScreenReply Screen { get; set; } }
public sealed class PackViews
{
    public PackViews UseScreen<TView>(string screenName) where TView : ComponentBase, IScreenView;
    public PackViews UseImport<TView>(string importName) where TView : ComponentBase, IImportView;   // F59.7
    public Type ScreenView(string screenName);   // the registered view, else ScreenTable
    public Type ImportView(string importName);   // the registered view, else ImportCard
}
```

**The routes,** all on the admin group, so the admin key covers them:

| Route | Answers | Child |
|---|---|---|
| `GET /v1/admin/screens` | Every screen's description, for the sidebar and the page | F59.5 |
| `GET /v1/admin/screens/{name}/rows?search=&page=` | One page of rows and the total | F59.5 |
| `GET /v1/admin/screens/{name}/rows/{id}` | One row as a form | F59.6 |
| `PUT /v1/admin/screens/{name}/rows/{id}` | Saves the form, or says which field was refused | F59.6 |
| `POST /v1/admin/screens/{name}/actions/{action}` | Does it, and says what happened | F59.6 |

### Architecture

```mermaid
flowchart LR
    pack["packs/louis-agent.pack.recipes: tools, stores and IDataScreen, no UI"] -->|lists screens| catalog["AgentCatalog"]
    catalog --> api["API: /v1/admin/screens and the rest"]
    api --> contracts["api.contracts: the JSON"]
    web["src/web/louis-agent.web: the frames, chat, generic screens, admin pages, base styles and scripts"] --> contracts
    packweb["packs-web/louis-agent.pack.recipes.web: RecipeLibraryView"] --> web
    packweb --> contracts
    puls["demos/puls/web/louis-agent.puls: its words, brand, landing, small layouts, thin chat page"] --> web
    puls --> packweb
    code["demos/louis-agent/web/louis-agent.code-review: its workspace, git and themes, thin chat page"] --> web
    starter["demos/starter/web: the smallest site, and the dotnet new template, F59.9"] --> web
    puls -->|admin key| api
    code --> api
    starter --> api
```

### Communication: fixing a recipe, then removing another

```mermaid
sequenceDiagram
    participant U as You
    participant M as PackMenu
    participant P as Screen page
    participant V as PackViews
    participant T as ScreenTable
    participant F as ScreenForm
    participant A as API (admin key)
    participant S as RecipesScreen
    M->>A: GET /v1/admin/screens
    A-->>M: descriptions, one link each
    U->>P: opens /admin/screens/recipes
    Note over P: AdminFrame hands over to the site's layout
    P->>V: ScreenView recipes
    V-->>P: ScreenTable, nothing registered
    P->>T: DynamicComponent, with Screen
    T->>A: GET rows?search=melktert&page=1
    A->>S: ReadAsync
    S-->>T: rows and the total
    U->>T: opens a row
    T->>F: the row's id
    F->>A: GET rows/{id}
    A->>S: ReadRowAsync
    S-->>F: the fields
    U->>F: fills the yield, adds a line, saves
    F->>A: PUT rows/{id}, with the values
    A->>S: SaveRowAsync, marked edited, and logged
    S-->>F: Done, or the field it refused
    U->>T: Remove, then Yes, remove it
    T->>A: POST actions/remove, with the row's id
    A->>S: ActAsync, and the action is logged
    S-->>T: Done, gone from the feed within five minutes
    T->>A: GET rows again
```

### Objects in one browser tab

```mermaid
flowchart TB
    host["WebAssemblyHost: one"] --> frames["SiteFrames: one, set at start"]
    host --> views["PackViews: one, filled at start, then only read"]
    host --> words["ChatWords: one, set at start"]
    frame["AdminFrame: one"] --> frames
    frame --> layout["The site's AdminLayout: one"]
    layout --> shell["AdminShell: one"]
    shell --> menu["PackMenu: one, holds the descriptions"]
    page["Screen page: one per open screen, holds a ScreenReply"] --> views
    page --> gate["AdminGate: only when the key was refused"]
    page --> dyn["DynamicComponent: one"]
    dyn --> view["RecipeLibraryView, or nothing if the default is used"]
    view --> table["ScreenTable: holds the search, the page number and the rows"]
    dyn -.-> table
    form["ScreenForm, on the row's page: holds the fields, their rows and any refusals"]
    menu --> api["AgentApi: shared"]
    page --> api
    table --> api
    form --> api
```

### Folder trees

Today, with only what changes:

```
src/web/louis-agent.web.shared/                        the shared parts, no pages, no styles
src/tests/web/louis-agent.web.shared.tests/
src/engine/louis-agent.composition/Packs/Reports/      IDataReport and its records
src/hosts/louis-agent.api/Endpoints/Admin/Reports/     the route and its handler
demos/louis-agent/web/louis-agent.web/                 the code-review app
demos/puls/web/louis-agent.puls/
  Admin/, Components/Admin/, Pages/Admin/              about 10,600 lines of admin
  Components/Chat/, Pages/ChatPage                     about 1,500 lines of chat
  Layout/                                              AdminLayout, MainLayout
  wwwroot/css/                                         everything, 20 files
packs/louis-agent.pack.recipes/Library/Usage/          RecipesUsageReport
```

After all the children:

```
src/web/louis-agent.web/                               the framework's web (renamed)
  Admin/, Components/Admin/                            AdminGate, AdminShell, the eval components
  Components/Chat/                                     ChatShell and its parts
  Components/Imports/, Components/Packs/, Components/Screens/
  Layout/                                              AdminFrame
  Pages/Admin/                                         Screen, ScreenRow, Imports, Evals, Review, Studio, Prompts, BenchmarkRun
  wwwroot/css/, wwwroot/js/                            the base styles and scripts
src/tests/web/louis-agent.web.tests/                   (renamed)
src/engine/louis-agent.composition/Packs/Screens/      new (Reports goes)
src/hosts/louis-agent.api/Endpoints/Admin/Screens/     new (Reports goes)
demos/louis-agent/web/louis-agent.code-review/         (renamed)
demos/puls/web/louis-agent.puls/
  Components/Brand/, Components/Landing/               what's Puls's own
  Pages/                                               LandingPage, thin ChatPage, NotFound
  Layout/                                              AdminLayout (small), MainLayout
  wwwroot/css/                                         tokens and landing only
demos/starter/web/louis-agent.starter/                 new: the smallest site, with .template.config/
packs/louis-agent.pack.recipes/Library/Screens/        new: the three screens
packs-web/louis-agent.pack.recipes.web/                new, beside packs-evals/
packs-tests/louis-agent.pack.recipes.web.tests/        new; sees both sides, to hold the duplicated name and columns
```

### Namespace map

Today's `LouisAgent.Web.Shared.*` becomes `LouisAgent.Web.*`, and today's `LouisAgent.Web.*` becomes `LouisAgent.CodeReview.*`, so the
code-review app is renamed first.

| Type | Folder | Namespace | Child, step |
|---|---|---|---|
| The code-review app, renamed | `demos/louis-agent/web/louis-agent.code-review/` | `LouisAgent.CodeReview` | F59.1, 2 |
| `web.shared` and its tests, renamed | `src/web/louis-agent.web/`, `src/tests/web/louis-agent.web.tests/` | `LouisAgent.Web`, `LouisAgent.Web.Tests` | F59.1, 3 |
| `FrameworkWebNamesNoSiteTests`, `SiteLayoutsStaySmallTests` | `src/tests/engine/louis-agent.core.tests/Docs/` or beside `RazorHasNoCodeTests` | as the folder | F59.1, 4 |
| `SiteFrames`, `AdminShell` | `src/web/louis-agent.web/Components/Admin/` | `LouisAgent.Web.Components.Admin` | F59.2, 1 and 2 |
| `AdminFrame` | `src/web/louis-agent.web/Layout/` | `LouisAgent.Web.Layout` | F59.2, 1 |
| `ChatShell`, `ChatWords`, and what moves from `Components/Chat/` | `src/web/louis-agent.web/Components/Chat/` | `LouisAgent.Web.Components.Chat` | F59.3 |
| Base styles and scripts | `src/web/louis-agent.web/wwwroot/` | none | F59.4 |
| `IDataScreen` and its F59.5 records; `FakeScreen` | `src/engine/louis-agent.composition/Packs/Screens/`; `src/tests/shared/louis-agent.testing/` | `LouisAgent.Composition.Packs.Screens`; `LouisAgent.Testing` | F59.5, 1 |
| `ScreenEndpoints`, `ScreenHandlers` | `src/hosts/louis-agent.api/Endpoints/Admin/Screens/` | `LouisAgent.Api.Endpoints.Admin.Screens` | F59.5, 2 |
| `ScreensReply`, `ScreenReply`, `ScreenColumnReply`, `ScreenPageReply`, `ScreenRowReply` | `src/hosts/louis-agent.api.contracts/Admin/Screens/` | `LouisAgent.Api.Contracts.Admin.Screens` | F59.5, 2 |
| `ScreenTable`, `PackMenu` | `src/web/louis-agent.web/Components/Screens/` | `LouisAgent.Web.Components.Screens` | F59.5, 3 and 6 |
| `PackViews`, `IScreenView` | `src/web/louis-agent.web/Components/Packs/` | `LouisAgent.Web.Components.Packs` | F59.5, 4 |
| `Screen` (the page) | `src/web/louis-agent.web/Pages/Admin/` | `LouisAgent.Web.Pages.Admin` | F59.5, 5 |
| `RecipesUsageScreen` | `packs/louis-agent.pack.recipes/Library/Screens/` | `LouisAgent.Packs.Recipes.Library.Screens` | F59.5, 7 |
| `RecipeLibraryView`, `RecipeWebViews`, their tests | `packs-web/louis-agent.pack.recipes.web/`, `packs-tests/louis-agent.pack.recipes.web.tests/` | `LouisAgent.Packs.Recipes.Web`, `.Tests` | F59.5, 8 |
| `ScreenAction`, `ScreenActionOn`, `ScreenRecord`, `ScreenField`, `ScreenFieldKind`, `ScreenValues`, `ScreenAnswer` | `src/engine/louis-agent.composition/Packs/Screens/` | `LouisAgent.Composition.Packs.Screens` | F59.6, 1 and 2 |
| `ScreenForm`, `ScreenRowsEditor`, `ScreenRow` (the page) | `src/web/louis-agent.web/Components/Screens/`, `Pages/Admin/` | as above | F59.6, 4 and 5 |
| `IRecipeStore`, `MongoRecipeStore`, `StoredRecipeDocument`, `IExcludedPages`, `MongoExcludedPages` (changed) | `packs/louis-agent.pack.recipes/Library/Store/`, `Library/Excluded/` | as today | F59.6, 6 |
| `RecipesScreen`, `ExcludedPagesScreen` | `packs/louis-agent.pack.recipes/Library/Screens/` | `LouisAgent.Packs.Recipes.Library.Screens` | F59.6, 7 to 9 |
| `ImportCard`, `ImportForm`, `Imports`, `IImportView` | `src/web/louis-agent.web/Components/Imports/`, `Pages/Admin/`, `Components/Packs/` | as the folder | F59.7 |
| The eval pages and components | `src/web/louis-agent.web/Components/Admin/`, `Pages/Admin/` | as the folder | F59.8, its own plan |
| The starter site | `demos/starter/web/louis-agent.starter/` | `LouisAgent.Starter` | F59.9 |

## Nine children, each its own branch and pull request

Each is reviewable alone, and all are free: no model is called. Today's branch has step 1 and isn't pushed, so it becomes
`feature/F59.1-name-and-rule`. Each child ends with Puls working as it did, so we can stop after any of them. Detail for F59.8 and
F59.9 is an outline, and each gets its own plan when it starts.

### F59.1 The name and the rule (5 steps)

**Done when** the projects have their new names, every site still builds and its tests pass, and the rule is written and held by tests.

| # | Step | Its test |
|---|---|---|
| 1 | **`AdminGate` moves to the shared library.** Committed | Already green |
| 2 | **The code-review app becomes `louis-agent.code-review`,** with Rider's rename: project, namespace, solution, docker, compose | Whole suite; the image builds |
| 3 | **`web.shared` becomes `louis-agent.web`,** with its tests, the same way | Whole suite; the Puls and code-review images build |
| 4 | **The rule's tests.** The framework names no site; a site's layouts stay small | Seen failing on a throwaway `Puls` word and on a long layout. Anything already in the shared library that breaks it is fixed here |
| 5 | **The rule and its docs.** The ADR, `src/docs/CLAUDE.md`, `.claude/rules/razor.md`, the project docs, the write-up | Links and size tests |

### F59.2 The admin frame (4 steps)

**Done when** Puls's admin looks and works as it does today, from `AdminShell` in the framework and a small layout in Puls.

| # | Step | Its test |
|---|---|---|
| 1 | **`AdminFrame` and `SiteFrames`.** The handing-over layout | It renders the named layout around the page; a missing setting says so plainly; no chat layout wraps it |
| 2 | **`AdminShell`.** The frame with its slots | Each slot draws what it's given; empty slots leave no gaps; the body shows |
| 3 | **Puls fills it.** A small `AdminLayout`, and the sidebar's links in a slot for now | The same links as today, in the same order, to the same addresses; the layout is under the size rule |
| 4 | **The docs and a look.** `louis-agent.web`'s project doc | Links and size tests, then a look in the browser beside today's |

### F59.3 The chat frame (8 steps, an outline until it starts)

**Done when** Puls and the code-review app each have a thin chat page on `ChatShell`, and each behaves as it does today.

| # | Step | Its test |
|---|---|---|
| 1 | **`ChatWords`.** Each word the frame shows, English by default, and Puls's in Afrikaans | Puls's words match today's, one by one; the framework has none of them |
| 2 to 4 | **The parts move in small groups:** the sidebar, the header, then the panels, each taking its words and brand from outside | Their existing tests move and stay green |
| 5 | **`ChatShell`.** The page's frame, state and handlers, with slots | A chat runs through it with a fake API; each slot draws; a slot left empty leaves no gap |
| 6 | **Puls's page goes thin.** Its addresses and slots only | Every address still works; the page test stays green |
| 7 | **The code-review app goes on it,** the second real user | Its workspace and git panels still work |
| 8 | **The docs and a look** | Links and size tests, then each site in the browser |

### F59.4 The base styles and scripts (4 steps)

**Done when** both sites look as they do today, with the shared rules coming from `_content/louis-agent.web/`.

| # | Step | Its test |
|---|---|---|
| 1 | **One colour contract.** Puls's rules move from its names (`--orange`, `--ink`) to louis-agent's (`--accent`, `--bright`), and Puls sets those to its own values | A look beside today's; each file stays under 200 lines |
| 2 | **The move.** Frame, chat and admin styles go to the framework, with `retro` and `paper`, and `retro` is the default. The landing's stay | The byte-checked move from `.claude/rules/css.md`, and the cascade's order is kept |
| 3 | **The scripts.** `pulsUi` gets a framework name and moves | The rule's test now covers scripts; Puls's bUnit tests stay green |
| 4 | **The code-review app uses the framework's themes, then the docs and a look** | Its theme switcher still works; screenshots beside today's for both sites |

### F59.5 Described screens (10 steps)

**Done when** the Puls sidebar lists the recipes counter from the pack, it opens through the generic page with the pack's own line
above the table, a test shows a demo's override winning, and `IDataReport` is gone.

| # | Step | Its test |
|---|---|---|
| 1 | **The seam.** `IDataScreen` and its records, `IAgentPack.Screens`, and `AgentCatalog.Screens`, which refuses a repeated name | The catalogue lists a pack's screens in order and refuses a repeat |
| 2 | **The routes.** List and rows, with the contracts | No key gets 401; the list comes back; search and page reach the screen; an unknown screen gets 404; the routes snapshot |
| 3 | **`ScreenTable`.** Each kind of cell, search, paging, and the empty and unknown cases | Each kind draws right; null shows as unknown; search asks again; a screen that can't be searched has no box |
| 4 | **`PackViews`.** A name to a component, read with `DynamicComponent` | An unknown name gets the default; a registered one gets its view; the last wins |
| 5 | **The generic page,** with `AdditionalAssemblies` on each site's router | It asks for the description and renders the view; a refused key shows the gate; an unknown screen says so; no route clashes |
| 6 | **`PackMenu`** in `AdminShell`, filling again after a key is saved | A link per screen; no links on a refused key, and no error |
| 7 | **The counter becomes a screen.** `RecipesUsageScreen`, and `IDataReport`, its route, `ReportCard` and the Reports page go | The same numbers as today's report tests; a repo-wide test finds no `IDataReport` |
| 8 | **The pack's own view.** `packs-web/louis-agent.pack.recipes.web` with `RecipeLibraryView` | The line reads *Of 10 offered, 1 served*, is absent with no rows, and the table stays; a test seeing both sides holds the name and columns |
| 9 | **The rule that holds it.** Nothing under `packs/` references the web, and nothing under `packs-web/` references a pack | Seen failing on a throwaway reference |
| 10 | **The docs.** A new ADR replacing 0083, `build-a-pack.md`, the project docs, the write-up, which covers MCP Apps | Links and size tests, then the free check below |

### F59.6 Recipe management (10 steps)

**Done when** you can search the library, open a recipe, change any of its fields, remove one, and clear an excluded page, all from the
screen, with the scripts no longer needed.

| # | Step | Its test |
|---|---|---|
| 1 | **Actions.** `ScreenAction`, `ActAsync` and the `POST` route, logged | No key gets 401; an unknown action gets 404; the answer comes back; the log names the action and the row |
| 2 | **One row as a form.** `ReadRowAsync`, `SaveRowAsync` and their routes | The fields come back in order; an unknown row gets 404; a refusal names its field; a save is logged |
| 3 | **Buttons.** Action buttons, the two-click confirm, the answer line, a reload after | A destructive action does nothing on the first click; a failure shows its reason |
| 4 | **`ScreenForm`.** Text, number and link, with read-only, required and refusals, on the row's own page | Each kind draws and sends its value; a read-only field can't change; a refusal shows beside its field |
| 5 | **`ScreenRowsEditor`.** A row per line, with add, remove and move | Each button does what it says; the order sent is the order shown |
| 6 | **The stores.** `IRecipeStore.RemoveAsync` and `ReplaceAsync`, with when it was edited; `IExcludedPages.AllAsync` and `RemoveAsync` | Real-Mongo integration tests: each does what it says, a missing id says so, and an old copy with no edit date reads fine |
| 7 | **`RecipesScreen`: the list.** Title, site, yield, when read, whether edited; search | Search finds by title and by site; no yield shows as unknown; an edited recipe says so |
| 8 | **`RecipesScreen`: the form.** Every field, and the recipe's rules | A recipe with sections goes out and back unchanged; a name, an ingredient and a step are required; the address and the site can't change; a later read doesn't overwrite the edit |
| 9 | **Remove and clear.** `ExcludedPagesScreen`, and the remove action | A removed page is refused on the next read and isn't kept again; once cleared it's read and kept fresh; the answer says when it leaves the feed |
| 10 | **The docs.** ADR 0082 amended, the recipes pack's doc, the write-up | Links and size tests, then the free check below |

### F59.7 Uploads the same way (4 steps)

**Done when** Uploads works from the framework at its own address, reached from `PackMenu`, and a test shows an override winning.

| # | Step | Its test |
|---|---|---|
| 1 | **The move.** `ImportCard`, `ImportForm` and the page | The existing tests move and stay green; a card per upload; none says so; a refused key shows the gate |
| 2 | **The override.** `IImportView` and `UseImport` | An unknown name gets `ImportCard`, a registered one its view, and the last wins |
| 3 | **The sidebar.** `PackMenu` shows Uploads when a pack has one | Shown with an upload, hidden with none |
| 4 | **The docs** | Links and size tests |

### F59.8 The eval pages (its own plan, about 10 steps)

**Done when** Evals, Review, Studio, Prompts and the run pages work from the framework, and Puls's admin layout is only its brand.
It starts by proving nothing in them is Puls's own, then moves them in small groups. Page titles take the site's name from outside.

### F59.9 Spawning a site (about 4 steps)

**Done when** `dotnet new louis-site -n Example` makes a site that builds and opens with a chat and an admin, in louis-agent's colours. This child also deletes
this plan, once its lasting parts are in the ADRs and the write-up. Steps: the starter site in `demos/starter/`; `.template.config`;
a test that creates one and builds it; and `guides/spawn-a-site.md`.

## Approved during the build

- **2026-11-06, the base colours are the site's Console theme, not `retro`.** Louis: the base stylesheet's colour scheme is that of the
  static website, `louis-agent.site`. So a site with no colours of its own is black and white with metallic grays, set in Geist, as
  `settings/tokens.css` there has it. This replaces what decision 10, *The look* and F59.4 say about `retro` and `paper` being the
  framework's: those two stay the code-review app's own themes, as a site's colours do. The variable names stay the code-review app's
  contract, because its rules already use only them. Two points are put to Louis when F59.4 starts: the Console theme has no colour for an
  error or a warning, and how the site and the framework share its values.

- **2026-11-06, Puls and the code-review app use the same framework.** Louis: "pulse and code review must use the same framework". So each
  child's done-when covers both sites: what moves into `louis-agent.web` is what both use, neither keeps its own copy of a part the
  framework has, and each child ends with both working. The rule's first line gains: *every site is built on it.* Whether the code-review
  app shows the admin frame and pack screens, which it has no pages for today, is put to Louis when F59.2 starts.

- **2026-11-06, the code-review app adapts to Puls.** Louis: "code review must adapt to puls". Where the two differ, Puls's shape is the one
  that goes into the framework, and the code-review app changes to fit it. So `ChatShell` comes from Puls's `ChatPage` and its parts, the
  code-review app's workspace and git panels become what it puts in the slots, and its own frame goes. In F59.4 Puls's rules are the ones
  that move, under the neutral variable names, and the code-review app's own copies of them are deleted; `retro` and `paper` stay as its
  colours. Colours aside, F59.3 and F59.4 stop to show Louis anything the code-review app loses in that change.

- **2026-11-06, the git controls move to the git pack's web project, as recipes' do.** Louis: "these control must move to the git pack web
  project like recipes". So `packs-web/louis-agent.pack.git.web` holds the Changes, Branches and History panels and what they need, and the
  code-review app keeps only its words, colours and which pack views it uses. This brings `packs-web/` forward from F59.5 to F59.3. Where
  the file browser goes, and how a pack's panel reaches `ChatShell`, are put to Louis when F59.3 starts.
- **2026-11-06, the summaries the rename's files lacked come in their own commit straight after it,** Louis's pick A, so the rename stays
  easy to review. It adds one step to F59.1.

- **2026-11-06, the code-review app gets the admin frame too, from F59.5.** Louis's pick A: "because the git things must live there". So it
  gets a small `AdminLayout`, the `SiteFrames` setting and the admin pages as they move in, and the git pack's controls live in that admin.
  How they appear there is put to Louis when F59.3 starts.

- **2026-11-06, everything in `louis-agent.web` is in English; only Puls is in Afrikaans.** Louis's words. So `ChatWords` in F59.3 is English
  in the framework, and Puls fills its own in. The admin stays English on every site.

- **2026-11-06, every pack works the same way, recipes too; the git and file controls get a child of their own.** Louis's picks A and A on
  decisions 13 and 14: "all packs will work the same recipes also". A pack names its screens through the one seam, and its real components
  come from its own project under `packs-web/`, found by name. So the git pack's are in `louis-agent.pack.git.web` and the file browser in
  `louis-agent.pack.files.web`, as admin screens. They need the override list, so they come after described screens, and no pack puts a
  panel in the chat, which makes `ChatShell` simpler. `ChatText` grows rather than a new `ChatWords`. The children's numbers from here:
  F59.3 the chat frame, F59.4 base styles, F59.5 described screens, **F59.6 the git and file controls (new)**, F59.7 recipe management,
  F59.8 Uploads, F59.9 the eval pages, F59.10 spawning a site. Where the sections above use the old numbers, these win.

- **2026-11-06, the code-review app's move is its own child.** Louis's pick, since F59.3 had grown past about ten steps. F59.3 is Puls's half:
  `ChatShell`, with Puls's page thin, merged first. The children's numbers from here: F59.3 the chat frame, **F59.4 the code-review app on the
  frame (new)**, F59.5 base styles, F59.6 described screens, F59.7 the git and file controls, F59.8 recipe management, F59.9 Uploads, F59.10
  the eval pages, F59.11 spawning a site. Where earlier text uses other numbers, these win.

- **2026-11-06, `ChatShell` and its parts sit by concern.** As Louis reviewed each step of F59.3: `Components/Chat/Shell/` for the frame and its
  two records, a folder each for the sidebar, header, search, composing, turns and panels, and the state classes in `Shell/`, not all in
  `Components/Chat/` as the namespace map says. F59.3 took twelve steps with the state classes, each its own commit.

- **2026-11-06, the code-review app moves last of the three, and its git and file controls go straight to admin screens.** Louis's picks
  on the code-review move. The frame needs scripts and styles that only Puls has, so base styles and scripts come first (1A). Files, Changes
  and Branches go straight to admin screens with no stop in the chat's sidebar (2C), so they come before the app's chat page moves, and the
  app keeps its old frame and tabs until then. A site's page drives the chat through a few public methods on `ChatShell`, a Facade (3A). What
  doesn't suit a site is turned off with settings and slots that are on by default, Template Method hooks, and the theme picker gets a
  settings slot (4B). The children's numbers from here: F59.4 base styles and scripts, **F59.5 described screens, F59.6 the git and file
  controls, F59.7 the code-review app on the frame**, F59.8 recipe management, F59.9 Uploads, F59.10 the eval pages, F59.11 spawning a site.
  Where earlier text uses other numbers, these win.
  Two things follow. F59.4 is done when Puls looks as it does today from the framework's files; the code-review app can't load them yet, since
  its own frame uses the same class names, so its own copies are deleted in F59.7, and `retro` and `paper` move then too. And a Review button on
  an admin screen is on another page from `ChatShell`, so 3A's methods can't reach it from there: how it gets to the chat is put to Louis when
  F59.6 starts.

- **2026-11-06, the static site becomes the coding agent's landing page.** Louis: "the static site must become the landing page for the coding
  agent, it will be it's demo". So `louis-agent.site` stops being outside the rule, and ADR 0084 changes when it moves. The Console colours live
  in the framework as the one copy, and the site loads them when it moves; until then its own file stays as it is, with no test between the two,
  since that copy is going. It's its own child, **F59.12**, with its own plan, including whether it stays server-rendered and where its address sits
  beside the chat's.

- **2026-11-06, the base theme's errors and warnings, and the admin's styles.** Louis's picks on F59.4. The Console theme gains one muted red and
  one amber, used only for errors and warnings (1A), in the framework's copy. All seven admin stylesheets move in F59.4, as this plan first
  said (4B), so the framework holds the styles of the eval pages before their pages arrive in F59.10.

- **2026-11-06, Bootstrap's own variables are the colour contract.** Louis's pick A on "we must bootstrap it". Every site loads Bootstrap, and rules
  use its names. This replaces what decision 10 and the first note above say about the code-review app's names being the contract, so `retro` and
  `paper` are rewritten in F59.7. What Bootstrap has no name for takes a `--web-` name. The landing's rules take the same names, so Puls keeps one
  list. The names, checked against Bootstrap 5.3.3's `:root`:

  | Puls today | What it is | From here |
  |---|---|---|
  | `--bg`, `--text` | The page and its text | `--bs-body-bg`, `--bs-body-color` |
  | `--ink`, `--muted`, `--dim` | Strong, quieter and quietest text | `--bs-emphasis-color`, `--bs-secondary-color`, `--bs-tertiary-color` |
  | `--surface`, `--sidebar` | Cards, and the menu | `--bs-secondary-bg`, `--bs-tertiary-bg` |
  | `--border` | Lines | `--bs-border-color` |
  | `--orange`, `--orange-text`, `--orange-soft` | Actions, their text, their tint | `--bs-primary`, `--bs-primary-text-emphasis`, `--bs-primary-bg-subtle` |
  | `--green`, `--green-light`, `--green-soft` | Your messages, done, their tint | `--bs-success`, `--bs-success-text-emphasis`, `--bs-success-bg-subtle` |
  | `--blue`, `--blue-soft` | Links and their tint, already the same values as the two greens | `--bs-link-color`, `--bs-success-bg-subtle` |
  | `--danger` | Errors | `--bs-danger` |
  | `--shadow-sm`, `--shadow-md` | Shadows | `--bs-box-shadow-sm`, `--bs-box-shadow` |
  | `--radius` | The big corner, 16 px | `--bs-border-radius-xl`, Bootstrap's own 1rem |
  | `--font`, `--font-code` | Type | `--bs-font-sans-serif`, `--bs-font-monospace` |
  | `--orange-deep`, `--green-deep`, `--navy`, `--navy-deep` | Darker steps, and the sidebar's foot | `--web-primary-deep`, `--web-success-deep`, `--web-sidebar-foot`, `--web-sidebar-foot-deep` |
  | `--cta`, `--shadow-glow`, `--code-bg` | The button's gradient, its glow, code blocks | `--web-cta`, `--web-glow`, `--web-code-bg` |
  | `--ease`, `--spring` | Motion curves | `--web-ease`, `--web-spring` |
  | `--cream` | Nothing uses it | Deleted |

  Layout values such as `--sidebar-w` stay. `--success` and `--mono` are never set, so those rules always use their fallbacks, and they stay as
  they are rather than change the look.

  F59.4's steps replace the section above, each its own commit:

  | # | Step | Its test |
  |---|---|---|
  | 1 | **Puls takes Bootstrap's names,** in its tokens, every rule and `EvalScorecard`, with nothing moved | A style test: no rule uses an old name. A look, since Bootstrap's own dropdowns, tables, cards and modal now read these values |
  | 2 | **No fixed colours in the shared rules,** added at Louis's question "can't we use color vairables?": about 250 were written as values, 89 of them Puls's orange, so any site would have come out orange. Each becomes a variable, an `-rgb` one where it's see-through, and white goes by what it's for | A style test: no fixed colour in base, layout, chat or admin. A look, with the old stylesheet swapped in |
  | 3 | **Sass, set up on Puls** with `AspNetCore.SassCompiler` and a new ADR. The files become `.scss` with their text unchanged, `app.scss` is the ordered list through `@use`, and the generated CSS isn't committed. Each colour is set once and a loop writes both its forms | The generated CSS matches today's, byte for byte where the text is unchanged; the Puls image builds with it; the `Stylesheet` helper, the size test and `.claude/rules/css.md` follow |
  | 4 | **The framework's base colours:** the Console theme under the new names, with its red and amber | A style test: every name the rules use is set; a site with no colours of its own opens black and white |
  | 5 | **The frame and chat styles move,** with the plain elements, in the same order, with the framework's colours first and the site's after | The byte-checked move; `PulsStylesheetTests`; a look |
  | 6 | **The seven admin stylesheets move** | The byte-checked move; a look at the admin's open pages |
  | 7 | **The scripts move:** `chatUi` and `louisChat`, and the `puls.*` browser settings take a neutral name, with no migration (ADR 0049). The rule's test now covers scripts and styles | Seen failing on a throwaway site word; Puls's page tests stay green |
  | 8 | **The docs and a look,** at desktop and phone width, beside today's | Links and size tests, then the browser |

  **Sass, Louis's picks.** He asked "maybe we should chnage to less? or something similar" and picked Sass over plain CSS. `AspNetCore.SassCompiler` compiles it, MIT, no Node (1A). Sass covers our own styles, and Bootstrap stays on its CDN (2A). CSS variables stay, since the code-review app swaps themes while it runs and a preprocessor's variables are fixed when it builds. Rules take nesting as they're touched, never in a sweep.
  **Always reuse,** Louis's words: "while building scss we must make sure to always reuse". Before writing a rule, look for a variable, mixin or partial that already does it; what repeats becomes one shared piece; a site `@use`s the framework's Sass and never copies it. Step 3 writes this into `.claude/rules/css.md`.
  **A site compiles the framework's Sass,** so there's no `_content/` stylesheet as this plan first said: a site lists the framework's partials under `web/` in its own `app.scss`, found through `IncludePaths`, and gets one CSS file with the order it always had.

- **2026-11-07, finished unattended on one branch.** Louis: "finish f59 unattended", with these answers. One branch and one PR at the end,
  `feature/F59-framework-web`, in place of a branch and PR per child. No Docker at all: each child is checked with `dotnet publish`, and Louis
  builds the images at the end. A Review or Ask button on an admin screen opens a new chat with the prompt as a draft, never sent by itself. The
  static site stays server-rendered, on the framework's colours and Sass, with the app at `/chat`. What the build chooses beyond this is recorded below
  as it comes, for Louis to read at the end. FX59, the mouse wheel stopping short after a chat, stays open and must be fixed before release.

- **2026-11-07, what F59.5 chose, unattended.** Each is the recommended option, for Louis to read at the end.
  - `ScreenTable` sits in `Components/Screens/Table/` with its cells, since the folder limit is six files. This differs from the namespace map.
  - `Above` is a `RenderFragment<ScreenPageReply>`, because a view's line has to read the rows, and it isn't drawn when there are no rows. So
    a view can't add something above an empty table, which nothing needs yet.
  - A page reply carries `PageSize`, so the browser doesn't repeat the number.
  - Only `http` and `https` values become links, since a value can come from a page we read. A column kind the browser doesn't know draws as text.
  - The search asks when you press its button, not as you type, so no pack is read per keystroke.
  - `FrameworkPages.Assemblies` is the one list a site's router takes, so a site doesn't name a framework type.
  - `ChatStore` gained an `AdminKeySaved` event, so `PackMenu` fills again after a key is saved.
  - The code-review app has its small `AdminLayout` and the framework's page from here, but its screens look unstyled until F59.7 moves its CSS.
  - `RecipeLibraryView` wraps the table (Decorator), and the screen's id and column names are duplicated on each side of the wire, held by a test.

- **2026-11-07, what F59.6 chose, unattended.** Each is the recommended option, for Louis to read at the end.
  - The git and files packs offer `OwnViewScreen`s: a name and a heading, no columns and no rows, since their views read the git and workspace
    routes. This keeps one seam rather than a second list of panels.
  - Puls's host loads the same two packs, so a screen with no columns is listed and opened only by a site that carries its view (`PackViews.CanDraw`).
    The alternatives were a setting per site naming its screens, or moving the packs out of Puls's host, which changes what Puls's model can do.
  - The file browser's screen is called `workspace-files`, as the plain `files` is the toolset's name and the repeated-names rule holds it to its enum.
  - `ChatHandoff` takes the site's chat address when the site builds it, and holds the draft in memory, not in the web address, so a long prompt stays
    out of the browser's history. The chat's title comes from the draft as for any message, so a screen's review no longer sets its own.
  - Inside each web project the code sits by concern, `Changes/` and `Branches/` with `Views/` for the panels, as the six-file limit asks.
  - The app's sidebar tabs keep working from the moved components until F59.7 removes them, as Louis's 2C says.

- **2026-11-07, what F59.7 chose, unattended.** Each is the recommended option, for Louis to read at the end.
  - The hooks are one record, `ChatFeatures`, rather than a parameter each, so the frame's parts take one value down. *Unfinished* covers everything that only says it's coming, the profile menu included.
  - The Facade methods from pick 3A shrank to the one that already existed, `SetDraftAsync`: with Review opening a draft and the panels on their own screens, the page has nothing else to drive (ADR 0055).
  - The chat is at `/chat`, with `/` showing the same page. The sidebar links to the three packs' screens by name.
  - Themes are Sass too, built to `wwwroot/css/themes/`, so each colour is written once and gets both its forms. They're no longer in git, and `themes.json` stays.
  - The packs' rules are Sass in each pack's web project, listed by a site under `pack-`, found through `IncludePaths`. A pack's view needs a site to list them, which the starter will show.
  - Retro's bracketed buttons now apply only on a pack's screens, as the chat's send button is the frame's round one.
  - A reply that isn't JSON is now an `AgentApiException`, found on the first look: the dev server's web page crashed every admin page. Not in the plan, and it helps every site.
  - `SiteColoursTests` lists `--bs-border-radius` as Bootstrap's own, which the packs' small corners read.

- **2026-11-07, what F59.8 chose, unattended.** Each is the recommended option, for Louis to read at the end.
  - The spec gains `Actions` and `Editable` as properties with defaults, so a read-only screen says nothing about them.
  - The screens are called `kept-recipes` and `excluded-pages`, as `recipes` is the toolset's name.
  - A row's form opens from a link on its first cell. *Not edited* shows empty, since that's known, and an unknown yield says so.
  - Removing excludes the page first, then removes the recipe, so a failure can't leave it gone and ready to come back.
  - Both lists read everything and page in memory, as a few hundred recipes don't need a database query, and the feed already reads them all (ADR 0055).
  - `RecipeForm` is a plain class apart from the screen, so the recipe's rules test without a store. Steps 4 and 5 are one commit, as are 7 to 9, as they only work together.

- **2026-11-07, what F59.9 chose, unattended.** `ImportForm` moved beside `ImportCard` in `Components/Imports/`, not in a folder of its own. Puls's *Data* heading went with its only link, so Uploads now sits under the sidebar's *Packs*. The four steps are one commit plus the docs, as the page, the override and the link only work together.

- **2026-11-07, the order changed, and what F59.11 chose, unattended.** F59.11 and F59.12 go before F59.10. The eval pages are 271 files and 126 test files, each needing its namespace
  changed by hand or through Rider, as ADR 0052 allows no script, on a machine short of memory, and nothing later needs them: a new site's admin already has screens and Uploads.
  - The starter's landing page is one heading and a link, as a landing page is what makes a site itself.
  - It starts with credits, language, the place, projects and what's unfinished turned off, as they need more than a new site has.
  - The template replaces `LouisAgent.Starter` with the name, and `louis-agent.starter` with the name in lowercase. A site goes in `demos/{site}/web/{site}/`, since its project points at the framework by relative path.
  - The test uses `dotnet new`'s custom hive, so running it never touches this machine's template list.

- **2026-11-07, what F59.12 chose, unattended.** This stands in for its own plan, as Louis's answers settled the shape: server-rendered, the framework's colours and Sass, the app at `/chat`.
  - The site reads the framework's colours through `IncludePaths` and references no project of ours, so it still runs nothing of the engine. Its image copies the framework's `Styles` folder.
  - It loads no Bootstrap stylesheet, which departs from "every site loads Bootstrap": it reads the names, but a page with no script and no Bootstrap component only gets a reset that would move its layout. For Louis to overrule.
  - `CHAT_ADDRESS` says where the chat lives, `/chat` by default, since the site and the app are separate programs today.
  - Only the buttons changed: *Try the coding agent* leads the hero and *Try it* replaces *Early access* in the header. The rest of the words still speak about the framework, and they're Louis's to choose.
  - The films' fingerprint now covers the framework's colours too. It's red until they're exported, which needs Docker.
  - `Stylesheet`, the test helper for plain CSS, goes, as no site is on plain CSS any more.

- **2026-11-07, what F59.10 chose, unattended.** Louis allowed a one-off script for this move alone. It stands in for its own plan.
  - First everything Puls's own came out while the pages were still in Puls, then the script moved them and changed only namespaces, usings and paths, and a checker that doesn't share the script's mapping proved it: 402 files, 102 byte for byte, 300 with 653 lines, nothing else.
  - Three more choices the plan doesn't settle: `Components/Admin` is full, so the moved parts go in `Components/Admin/Evals/`; `Pages/Admin` is full too and a namespace can't share its class's name, so the pages go by concern in `Running`, `Reviewing` and `Building`; `Admin/` keeps its name.
  - On screen it says *The agent itself*, *The agent with cook* and *Agent*, and the store's title *The store answers*. For Louis to reword.
  - The shop list became data a site hands over: `SiteKinds`, a named rule per kind of website (Specification), Puls's in `PulsSites`. The other two options were a neutral count of websites, which loses what the chart is for, and a slot for the whole chart, which would have to be threaded through the explorer. The hover note speaks of kinds now, so Puls loses its sentences about SA shops.
  - `EvalPages.AddTo` is the one call, made by all three sites, as a router finds every page in the framework. `EvalLinks` is only in Puls's frame, so the other two have the pages with nothing leading to them, for Louis to turn on.
  - `web/admin/eval-pages` forwards the six partials, so three sites list one name. Reused: all six partials as they were. New: only that list.
  - Test data still says `puls-cook` in the moved tests, as a sample name. Their long comment lines stay, so the diff stays a move; they're on the TODO.

## Verification

**Free, each child:** the whole unit suite and every site's image builds. **In a browser:** each child ending in a look compares a site
with how it looked before. With your admin key, the screens open from the sidebar and show the numbers, a search works, a recipe
is edited, and another is removed and comes back when cleared. I won't type your key, so that part is yours or we do it together.

**No paid run,** as nothing here calls a model. Editing or removing a recipe does change what the cook is offered, so the next long
run's numbers would reflect those changes.

**Puls isn't rebuilt while a run you want is going.**

## Concepts this teaches

- **Skill 20, extensibility:** what's framework and what's product, on the web as in the engine; a seam with an order of precedence; a
  vocabulary with a rule for growing.
- **Skill 11, protocols:** MCP Apps, what it standardises, and why it doesn't fit here, in the write-up because you asked to learn it.
- **Skill 15, safety:** a screen can do only what its pack named, destructive actions take a confirmation, and none of this is a tool
  a model could call.
- **Skill 18, testing and shipping:** a rule held by a test, and a template proved by building what it makes.
- **Metadata-driven UI, Bridge and Composition Root:** one description drawn many ways, a layout handing over to the site's, and one
  `Program.cs` naming what's used.

## Left out, on purpose

- **A landing page in the framework:** it's what makes a site itself. The starter gets a plain one.
- **`louis-agent.site`,** the agent's website: it's a server-rendered site with no chat or admin, so it stays as it is for now.
- **A NuGet package for the framework or the template:** the template works inside this repo until packages exist.
- **Serving a screen as an MCP App.** A description is plain data, so it could be drawn that way later. No plan names it (ADR 0055).
- **Bulk selection, and other field kinds such as dropdowns or dates:** not in the vocabulary until a second screen needs them.
- **Swapping a view without a rebuild:** a custom component is built in, though a description isn't.

## Sources

[ABP themes](https://abp.io/docs/latest/framework/ui/blazor/theming) ·
[ABP menu contributors](https://abp.io/docs/latest/framework/ui/blazor/navigation-menu) ·
[Razor class libraries, Microsoft](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/class-libraries?view=aspnetcore-10.0) ·
[Blazor routing, Microsoft](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/routing?view=aspnetcore-10.0) ·
[Blazor layouts, Microsoft](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/layouts) ·
[DynamicComponent, Microsoft](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/dynamiccomponent) ·
[`dotnet new` templates, Microsoft](https://learn.microsoft.com/en-us/dotnet/core/tools/templates) ·
[Django admin](https://docs.djangoproject.com/en/5.2/ref/contrib/admin/) ·
[Django admin actions](https://docs.djangoproject.com/en/5.2/ref/contrib/admin/actions/) ·
[Filament resources](https://filamentphp.com/docs/4.x/resources/overview) ·
[Backstage extensions](https://backstage.io/docs/frontend-system/architecture/extensions) ·
[Grafana plugin types](https://grafana.com/developers/plugin-tools/key-concepts/plugin-types-usage) ·
[MCP Apps](https://modelcontextprotocol.io/docs/extensions/apps) ·
[Bridge, Refactoring.Guru](https://refactoring.guru/design-patterns/bridge) ·
[Plugin, Fowler](https://martinfowler.com/eaaCatalog/plugin.html) ·
[Registry, Fowler](https://martinfowler.com/eaaCatalog/registry.html)

---
[Plans](README.md) · Related: [the TODO](../../../TODO.md)
