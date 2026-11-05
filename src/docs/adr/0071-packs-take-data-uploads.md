# 0071. Packs take data uploads, previewed before they are saved

**Status:** Accepted · **Date:** 2026-11-04 (Louis: "I need to upload it via admin", "cause they do not release one
every month", "we need to pick up the corrections"; picked *Packs declare imports*, *a names file by Stats SA's code*,
*every month in the sheets, the preview lists the corrections* and *an older month fills a gap when read*)
· **Recorded:** 2026-11-04

## Context

The shop-prices pack estimates from Stats SA's average prices (P0141, [ADR 0069](../../../packs/docs/adr/0069-shop-prices-are-stats-sa-estimates.md)),
kept in MongoDB one document per source and month. A new month was a JSON file made by hand and shipped in a build.
Stats SA doesn't release every month, and each release repeats every month back to January 2017, sometimes with an
earlier month corrected. So a scheduled refresh would run for nothing most months and miss the corrections, and a
build per release is a developer's job for what is an operator's task. The API may not name a pack
([ADR 0027](0027-engine-identities-and-packs.md)), and a pack stays convertible to MCP ([ADR 0028](0028-packs-stay-convertible-to-mcp.md)).
Three ways were weighed for where the upload lives:

- **A Stats SA route in the API**: the API would reference the pack, which ADR 0027 forbids.
- **A command-line tool** that writes the store: no build, but a person at a terminal with the store's secret.
- **Packs declare imports**, as they publish feeds ([ADR 0070](0070-packs-publish-data-feeds.md)): core names an
  upload (its files, a preview, a save), a pack lists its own, and the admin portal shows every upload the catalogue
  holds without knowing whose it is.

## Decision

*A pack can declare data uploads; the admin portal previews what the files hold and change, and saves only what the
person saw.*

- **`IDataImport`** in core (`Agent/Catalog/Imports/`): a `Name`, a `Description`, the `Files` it takes (an
  `ImportSlot` each), `PreviewAsync` and `SaveAsync`, both given the files (`ImportFile`) and both answering an
  `ImportReport` (the period, how many changes, whether saved, facts to show, problems). `IAgentPack.Imports` is empty
  unless a pack says otherwise; `AgentCatalog.Imports` and `Import(name)` gather them and refuse a name twice.
- **Admin routes, behind the admin key**: `GET /v1/admin/imports`, `POST /v1/admin/imports/{name}/preview` and
  `/save`, the files as base64 in the JSON body (20 MB at most). A save posts the same files again, so the server
  keeps no half-done upload between the two calls.
- **The preview is the check** (human in the loop): it lists the new months, the corrected ones with the prices that
  changed, and the unchanged; Save writes the new and the corrected, nothing else, and a second upload of the same
  release changes nothing. No tick to replace a month: what the preview shows is what Save does.
- **The first upload is `shop-prices`**: Stats SA's two spreadsheets (all urban areas, provinces), read by column
  name with ExcelDataReader; every month in them becomes a price file. The names a list may use for a product, which
  the sheets don't hold, are kept in `statssa-names.json` by Stats SA's product code; a product it lacks gets its
  tidied name and is listed in the preview.
- **An older month fills a gap when read**: a product the newest month lacks is priced from the newest month that
  has it, and the estimate names that month. Nothing is copied into a newer month, so a stored month is always what
  Stats SA published.

## Consequences

- A new release, or a correction, is an operator's upload: no build, no developer, no schedule.
- Another pack's data (a retailer's feed, if one ever agrees) is another `IDataImport`, with no change to the API or
  the portal.
- Upload is a portal job; an MCP server has no portal, so an import isn't served over MCP. The pack still converts:
  its tools and feeds do.
- The whole file travels twice (preview, then save). At two spreadsheets of a few megabytes that is fine; a large or
  slow upload would need the server to hold the preview, which is a new decision.

## Links

[ADR 0027](0027-engine-identities-and-packs.md) · [ADR 0069](../../../packs/docs/adr/0069-shop-prices-are-stats-sa-estimates.md) ·
[ADR 0070](0070-packs-publish-data-feeds.md) · [louis-agent.pack.shop-prices](../../../packs/docs/projects/louis-agent.pack.shop-prices.md) ·
[Write-up 16](../learning/16-human-in-the-loop.md#f55-the-preview-is-the-check-2026-11-04)
