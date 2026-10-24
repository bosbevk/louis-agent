# Add a shop

> **How-to.** Add a retailer to `packs/louis-agent.pack.shops`, so an agent with the `shops` toolset can price a list
> at that shop from its own catalogue: check you may, write its catalogue, register it, test it on recorded answers,
> and turn it on. **Elsewhere:** how the pack works in [louis-agent.pack.shops](../projects/louis-agent.pack.shops.md);
> building a pack from scratch in [Build a pack](build-a-pack.md).

The shops pack reads a shop's prices in code, from the shop's own catalogue, instead of letting a model read its pages.
Each shop is an `IShopCatalogue`. It reads the shop's site through the guarded browser (`IWebBrowser`), so the address
rule and the browser's honest user agent hold for it too ([ADR 0038](../adr/0038-the-browser-says-what-it-is.md)).
The tools (`FindProducts`, `SearchSpecials`, `SearchPromotions`) take the shop's name and work for every shop the
deployment enables. Checkers is the one shop today and the worked example: `Providers/CheckersCatalogue.cs`, and
`Providers/CheckersStore.cs` for the store that serves a town.

## 0. Check that you may

Read the retailer's terms first, and prefer a feed or an API they offer. A shop is read only where a deployment turns
it on (`SHOP_CATALOGUES`), so an instance that hasn't checked a shop's terms never reads it. Say in the catalogue's
summary what you checked, as `CheckersCatalogue` does.

## 1. Name it

Add a member to `Catalogues/ShopKind.cs`. The setting and the tools' `shop` parameter are matched to it with the case
ignored (`SettingChoice`, [ADR 0024](../adr/0024-choices-are-enums.md)), and the "supported shops" list comes from the
enum, so keep the name one word.

## 2. Write its catalogue, test first

1. Record what the shop really answers: its search page or catalogue answer for two or three queries, trimmed, saved
   under `tests/louis-agent.pack.shops.tests/Providers/Fixtures/`.
2. Write `{Shop}CatalogueTests` in `tests/louis-agent.pack.shops.tests/Providers/` against a fake `IWebBrowser` that
   returns the recording, as `CheckersCatalogueTests` does: the products, their sizes, the price now (a special's when
   lower), the price per kg for what's sold by weight, the link and the photo; an empty answer; a refusal.
3. Add `Providers/{Shop}Catalogue.cs` implementing `IShopCatalogue`: `Shop` and
   `SearchAsync(query, maxResults, place, cancellationToken)`, returning a `ShopSearch` of `ShopProduct`s. Visit the
   shop's pages as a shopper would, through the browser, and parse what comes back; don't call a private API
   directly.
4. A problem the agent can act on (no such product, the shop refused) is a `ShopCatalogueException` with its reason, a
   `ShopToolMessage` member ([ADR 0009](../adr/0009-message-text-as-enum-templates.md)), never a crash.
5. If the shop serves by area, find the store for the user's town once and cache it (`StoreSessionCache`), as
   `CheckersStore` does; without a place, search its default store and say so.

`CheckersCatalogue.cs` is over the 200-line limit and on `FileSizeTests`' list, so don't copy its size: split the
parsing from the visiting from the start.

## 3. Throttle it

Give the shop its own `ShopThrottle` (at most two searches at once, a pause after a refusal), one for the process, so
every engine's searches count against the same limit. Create it beside `CheckersThrottle` in
`Catalogues/ShopCatalogueFactory.cs`, and add the shop's arm to `Create`. `ShopCatalogueFactoryTests` gets a case.

## 4. Tell the model

- The tools' `shop` parameter descriptions in `Tools/ShopTools.cs` list the shops (`(checkers)` today): add the new
  one.
- The guide, `Prompts/toolsets/shops.md`, says which shops there are and when to use which tool: add the shop.
- Regenerate the pack's golden (`ShopToolMessageSnapshotTests`) if you added texts, and Puls's session golden
  (`PulsSessionSnapshotTests`), since the cook's tools are in it. Read both diffs.

## 5. Show its photos

The web apps show a model-written image only from a shop's own image host
(`src/louis-agent.web.shared/Text/Markdown.cs`, `ImageHosts`), so an injected image can't leak the chat. If the
shop's photos come from a host not listed there, add it with a case in `MarkdownTests`.

## 6. Turn it on and try it

```bash
dotnet test tests/louis-agent.pack.shops.tests tests/louis-agent.composition.tests
SHOP_CATALOGUES=checkers,{shop}     # in config/.env, or the Puls launch profile; the browser (BROWSER_URL) must be up
```

`PackConvertibilityTests` (composition tests) checks the tools still fit MCP's shapes. Then ask Puls's cook for a list
in a town the shop serves, and check each price against the shop's site. Once F18's evals run, the cook's eval measures
it.

## Before you ask for a review

- [ ] The retailer's terms are checked, and the catalogue's summary says what was checked.
- [ ] Tests run on a recorded, trimmed answer through a fake browser; no test reaches the shop.
- [ ] Failures are `ShopCatalogueException`s with their reasons; the shop has its own throttle.
- [ ] The tool descriptions, the guide and the goldens name the new shop.

---
[Guides](README.md) · [Docs index](../README.md)
