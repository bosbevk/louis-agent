# FX16 · A quote that is only a price was "Found" for any product at that price

> **Status:** done · **Area:** core, `WebTools.VerifyQuote` (F30) · **Found:** 2026-10-17 · **Fixed:** 2026-10-17

## Symptom

From the review of PR #51 (F30): `VerifyQuote(url, "R22.99")` answered *Found* on a page where R22.99 was the price of
another product. On a list of eggs (R22.99), butter (R54.99) and milk (R22.99), a minion's butter at R22.99 was
confirmed, so *Found* said more than was checked. Nothing uses `VerifyQuote` today (F30's prompts were restored), but
F31's shared scorer would have counted such a price as checked.

## Cause

`VerifyQuote` looked for the quote anywhere on the page, which is right for words ("Price valid until 20 Oct") and
says nothing for a bare price: a price names no product, and a shop's page holds many. The F30 minion's quotes were
often only that (`quote: "R22.99"`). Reproduced with the three-product list above: *Found* for the butter.

## Fix

`VerifyQuote(url, quote, product)`: a `product`, the name as the page shows it, ties the price to it.

- **A quote that is only a price needs its product.** Without one it is refused before any request
  (`QuoteOnlyAPrice`). "Only a price" is a quote with fewer than three letters left once its prices and the words
  that come with a price (`each`, `now`, `only`, `per`, `from`, `special`, `was`, `save`, the units) are taken out:
  `R22.99`, `R 22,99`, `R119.98/kg`, `R22.99 each`, `Now R22.99` (the PR #54 review: a filler word made a bare price
  look like words).
- **With a product, the quote must stand right after its name:** within 300 characters after one of the name's
  places on the page, with no other price between (`StandsAfter`). On a list the first price after a name is that
  product's; any later one is the next product's. A quote that holds the name itself is matched as a whole. A quote
  that is on the page but not after the name is *Not found with the product* (`QuoteNotWithProduct`), and the value
  is unconfirmed.
- **A special's old price or saving is skipped** (the PR #54 review): `Eggs 6 Pack · Was R29.99 · R22.99` has a price
  between the name and R22.99, and shops show *Was* and *Save* prices all the time. A price right after `was`, `save`
  or `normally` doesn't count as another product's, and a quote right after one of them is never the product's price
  (the eggs at R29.99 are *Not found with the product*).
- **A quote with words still works without a product,** as before. It can still be another product's ("ESKORT
  R65.99"), so a scorer that checks a price always passes the product: F31's scorer does.

Considered: a minimum quote length (a long quote can still be another product's: "ESKORT R65.99"); the price nearest
the name in either direction (on a list the previous product's price is as near); reading the page's structure (each
shop marks up its products differently, and a rendered page has no structure left).

## Tests

`VerifyQuoteTests`, written first and seen failing (five of 25) on a stub with the `product` parameter and no logic:

- `APriceOnlyQuote_WithoutItsProduct_IsRefused_WithoutARequest` (`R22.99`, `"R 22,99"`, `R119.98/kg`): was *Found*
  or *Not found* after a request.
- `AnotherProductsPrice_IsNotFoundWithTheProduct`: the butter at R22.99 was *Found*.
- `AProductThePageDoesntShow_IsNotFoundWithTheProduct`: was *Found*.
- `APriceRightAfterItsProduct_IsFound` (the eggs and the milk at R22.99) and `APriceSplitAcrossTags_StillMatches`
  passed before and after.

The PR #54 review's additions, also written first and seen failing (eight cases): the four filler quotes refused
(`R22.99 each`, `Now R22.99`, `only R22.99 per kg`, `Was R29.99 Save R7.00`), and on a page of specials
`ASpecialsOldPriceOrSaving_DoesntHideTheProductsPrice` (the eggs at R22.99 after *Was R29.99*, the butter at R54.99
after *Save R7.00*) and `TheOldPriceOrTheSaving_IsNotTheProductsPrice` (R29.99 and R7.00). The ninth,
`ASpecialStillDoesntLetTheNextProductsPriceThrough` (the milk's R22.99 for the butter), passed before and after.

The earlier tests that checked a bare price now name its product; the footer's price per kilo is quoted with its words
(`Unit price R71.98/kg`), and the absent quote has words (`Beef Mince R29.99`: `R29.99 per 500g` is now only a
price).

## Confirmed

By the tests only: no session uses `VerifyQuote` until F31 measures the minions. F31's first check that scores a quote
uses it with the product.

## Lesson

**A check is only as strong as what it binds** (skill 12, evaluation; skill 15, guardrails in code): "the words are on
the page" proves the words, and a price alone is a word every product at that price shares. A verifier has to tie the
claim to its subject (the price to its product) or say it can't, else a *pass* overstates what was checked, and every
score built on it does too.
