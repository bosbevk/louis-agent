# Third-party notices

> **Reference.** The parts of louis-agent that someone else wrote, and the licence each one keeps. The repo's own
> [LICENSE](LICENSE) (all rights reserved) covers only what the copyright holder wrote; everything below stays under
> its own licence, and its notice goes with it.

Why this file exists: Puls and the review web app run as Blazor WebAssembly, so their DLLs are downloaded into every
visitor's browser. That counts as handing the code out, and MIT, BSD and Apache all ask that their notice travels with
it ([ADR 0016](docs/adr/0016-licence-all-rights-reserved.md)). The exact version of each package is in the `.csproj`
that references it; the full licence texts are at the links.

## NuGet packages

| Package | Licence | Copyright holder |
|---|---|---|
| Anthropic | [MIT](https://licenses.nuget.org/MIT) | Anthropic |
| DocumentFormat.OpenXml | [MIT](https://licenses.nuget.org/MIT) | Microsoft |
| dotenv.net | [MIT](https://github.com/bolorundurowb/dotenv.net/blob/master/LICENSE) | Winner-Timothy Bolorunduro |
| ExcelDataReader | [MIT](https://licenses.nuget.org/MIT) | ExcelDataReader developers |
| Markdig | [BSD-2-Clause](https://licenses.nuget.org/BSD-2-Clause) | Alexandre Mutel |
| Microsoft.AspNetCore.Components.Web, .WebAssembly, .WebAssembly.Server, .WebAssembly.DevServer | [MIT](https://licenses.nuget.org/MIT) | Microsoft |
| Microsoft.Extensions.AI, .AI.Evaluation, .AI.OpenAI | [MIT](https://licenses.nuget.org/MIT) | Microsoft |
| Microsoft.Extensions.FileProviders.Composite, .Embedded | [MIT](https://licenses.nuget.org/MIT) | Microsoft |
| ModelContextProtocol, ModelContextProtocol.AspNetCore | [Apache-2.0](https://licenses.nuget.org/Apache-2.0) | ModelContextProtocol authors |
| MongoDB.Driver | [Apache-2.0](https://licenses.nuget.org/Apache-2.0) | MongoDB Inc. |
| OllamaSharp | [MIT](https://github.com/awaescher/OllamaSharp/blob/main/LICENSE) | Andreas Wäscher |
| OpenTelemetry, OpenTelemetry.Exporter.OpenTelemetryProtocol | [Apache-2.0](https://licenses.nuget.org/Apache-2.0) | OpenTelemetry Authors |
| PdfPig | [Apache-2.0](https://licenses.nuget.org/Apache-2.0) | UglyToad |
| PuppeteerSharp | [MIT](https://licenses.nuget.org/MIT) | Darío Kondratiuk |
| Serilog.Extensions.Logging, Serilog.Sinks.Console, .File, .OpenTelemetry | [Apache-2.0](https://licenses.nuget.org/Apache-2.0) | Serilog Contributors |
| YamlDotNet | [MIT](https://licenses.nuget.org/MIT) | Antoine Aubry |

The .NET runtime and base class library that ship with each app are Microsoft's, under the
[MIT licence](https://github.com/dotnet/runtime/blob/main/LICENSE.TXT). The test projects' packages (NUnit, bUnit and
the like) are only used to build and test, and are never handed out.

## Loaded by the web pages

The pages fetch these from a CDN at run time, so no copy is in the repo; their notice is inside each file served.

| What | Licence | Where |
|---|---|---|
| Bootstrap 5.3 (Puls) | [MIT](https://github.com/twbs/bootstrap/blob/main/LICENSE) | `src/web/louis-agent.puls/wwwroot/index.html` |
| highlight.js 11.11 (Puls, review web app) | [BSD-3-Clause](https://github.com/highlightjs/highlight.js/blob/main/LICENSE) | both apps' `wwwroot/index.html` |
| Fonts: Montserrat, JetBrains Mono, IBM Plex Mono, VT323, Geist, Geist Mono | [SIL OFL 1.1](https://openfontlicense.org) | Google Fonts, linked from the pages' CSS |

## Container images

The Docker images build on others' images (Microsoft's .NET and Aspire dashboard images, MongoDB, Chromium's
headless shell, SearXNG and Ollama, named in `docker/images/` and the compose files). Each keeps its own licence; we run them, we don't ship
them inside our code.

---
[Docs index](docs/README.md) · Related: [LICENSE](LICENSE) · [ADR 0016](docs/adr/0016-licence-all-rights-reserved.md)
