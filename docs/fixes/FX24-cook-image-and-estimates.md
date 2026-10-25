# FX24 · The cook's recipe photo and its estimates: the grader, the chip, the box and the prompt

> **Status:** done · **Area:** the cook eval (`recipe-image`, `CookScorer`,
> `Prices`), the admin portal's chips, Puls's stylesheet, the cook's prompt · **Found:** 2026-10-25, the first cook run on F18 step 5.7
> (`bd2138243795`, $0.076) · **Fixed:** 2026-10-26, on the F18 branch at Louis's override, with no plan file of its own

## Symptom

Louis, 2026-10-25/26: "images not showing"; "all of these we know it cannot price"; "we must also make sure we make the image a fixed
size to fit in our chat screen"; "i need to see here recipe-image". The cook's answer had no photo, three conversations failed
`recipe-image`, and every shopping list showed a red "priced 0 / 5" though the cook only gives estimates (no shop's prices are read, ADR 0038).

## Cause

- **The grader.** `recipe-image` v1 collected every image URL from every page the cook read, share-button icons included, and failed an answer
  that showed none of them. `cook-af` used a recipe page whose fetched text held no image; another page the cook had read on the way held
  the share icons. The grader said "the recipe page had one" and failed the conversation.
- **The chip.** The ratio and the chip counted only a shop's own linked price. An estimated row (`± R55`) was neither priced nor guessed, so
  a list of estimates read 0 of 5, in red.
- **The size.** Nothing sized an image outside a table cell: a 1024-wide photo filled the chat.
- **The prompt.** It asked for one image but not for a recipe page that has one.
- **The fetch.** Louis opened the cited pages: they do have photos. `HtmlText.ImageMarkdown` keeps an image only with alt text, and
  reads only `data-src` and `srcset`, and only inside the page's `main` or `article`. A photo with no alt, in `data-lazy-src`, in a `<picture>`, or
  in a header above the article is dropped, and the page's `og:image` and its JSON-LD `Recipe.image` are not read at all. The cook cannot show
  what its fetch never gave it.

## Fix

1. `recipe-image` is v2 (the `puls-cook` eval v4): it judges the images of the page the answer links to (found by the `URL:` line a fetched
   page starts with), and `Links.RecipePhotos` leaves out share buttons, print and PDF icons and logos. A cited page with no photo gives
   `Unknown`, never `Fail`. Re-scoring `bd2138243795` for $0 moved `cook-af`, `cook-en` and `cook-shops` from Fail to Unknown.
2. An estimated row counts as priced in the ratio and the chip (`PriceCount.Estimated`, the scores' `estimated`, `PricedChips`); `priced` stays
   the shop's own price, so `estimates-at-shop` is unchanged. The scores also carry `images` (the photos the answer shows), and the row has an
   `image shown` / `no image` chip (`ImageChip`).
3. The recipe photo is a fixed box, 480 × 320 px at most, cropped to fit, with 16px rounded corners (`.markdown img:not(td img)`).
5. **The fetch** (`LeadImage`, called by `HtmlText.ToText`, so FetchUrl and ReadRenderedPage both): the page's own photo is read from its JSON-LD `Recipe.image` (a string, an `ImageObject`, a list, or inside a `@graph`), else its `og:image` or `twitter:image`, made absolute from the page, and put first as `![title](url)`, unless the text already shows that file; a data URI, an SVG or a non-recipe JSON-LD gives none. A lazy image's file is also read from `data-lazy-src`, `data-orig-file` and `data-original`. The alt-less image rule stays (icons and spacers).
4. The cook's prompt drops a recipe page with no photo of the dish for the next result (three pages at most) and puts the photo on a line of
   its own above *Recipe*.
6. **AMP pages** (Louis, on `af.hiloved.com/maklike-kerrie-hoender-resep/`, the page `cook-en` cited): it has no `og:image`, no JSON-LD and no `<img>`; its photo is an `<amp-img alt="…">`, which the page text ignored. `<amp-img>` is read as an image (alt text still required).
7. **`within-budget` v2** (`puls-cook` eval v5): the shopping list's own bold total row (`**Totaal** … **± R323**`) is the basket's total (`TotalSource.ListTotalRow`, `Lists.TotalRows`). Version 1 read every line that said "total", and answers say it elsewhere too (a sentence, the unsure table) with the budget or a saving beside it, so the lines disagreed and the grade was `Unknown` (`cook-af`, `cook-malmesbury`). Per person is no total; two total rows that disagree are none.
8. **Estimates in a town with no shop** (Louis, on `cook-no-branch` and `cook-recipe-only`: "these ones did price and it is fine, it's estimates"): an estimated row is an estimate there too, not "no shop, no price"; the chip reads "priced 14 / 14 · estimated 14".

## How it is tested

The grader (version, the cited page, icons), the scorer, the chips, the style rule and the prompt each have tests; the golden file
`PulsSessionSnapshot.json` moved by one line. The estimates code and the chip class were written beside their tests before a red run.

## Confirmed

Re-score `ccec39cff66c` (free) and the live run `07fda82e72a7` ($0.078): no false image Fails remain; the conversations that still lack
a photo are `Unknown` because the fetch gave the cook none.
