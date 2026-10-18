"""Scoring shared by the checks (F31): deterministic code over a transcript, never a model, so a score is the same on
every rerun of the same answer. What a check scores is its own (benchmarks/{check}/); these are the parts more than
one needs. A price checked against its page goes through VerifyQuote with its product (FX16): a price alone matches
any product at that price."""
import re
from urllib.parse import urlparse

# The shops a cook's list prices at, by the name in a column's header, and their sites. A price links to its own shop.
SHOP_HOSTS = {
    "checkers": "checkers.co.za", "shoprite": "shoprite.co.za", "pick n pay": "pnp.co.za", "pnp": "pnp.co.za",
    "spar": "spar.co.za", "usave": "usave.co.za", "woolworths": "woolworths.co.za",
}
INLINE_LINK = re.compile(r"\]\((https?://[^)\s]+)")
REFERENCE_LINK = re.compile(r"\[([^\]]*)\]\[([^\]]*)\]")
REFERENCE = re.compile(r"^\s*\[([^\]]+)\]:\s*(https?://\S+)", re.MULTILINE)

RAND = re.compile(r"\bR\s?\d[\d\s,.]*")
ESTIMATE = re.compile(r"\b(unknown|onbekend|geskat|skatting|skattings|estimate[ds]?|nie bevestig|unverified|onbevestig|nie gevind)\b", re.IGNORECASE)
UNCONFIRMED = re.compile(r"\b(unknown|onbekend|geskat|skatting|skattings|estimate[ds]?|nie bevestig|unverified|onbevestig|snippets?|soekresultate|soekindeks)\b", re.IGNORECASE)
HEADER = re.compile(r"^\|\s*(Item|Bestanddeel|Ingredient|Produk)\b", re.IGNORECASE)
SEPARATOR = re.compile(r"^\|\s*:?-")
TOTAL = re.compile(r"^\|\s*\*\*")
# F34: the shops the cook has a tool for, by their sites (Checkers today, ShopKind in core), and the tools themselves.
TOOL_SHOP_HOSTS = ("checkers.co.za",)
SHOP_TOOLS = ("FindProducts", "SearchSpecials", "SearchPromotions")
DEAL_TOOLS = ("SearchSpecials", "SearchPromotions")
# A deal beside a price, as the shop words it: after the price's link (`[R24,99](…) · BUY 2 FOR R45`), or its badge.
DEAL = re.compile(r"\)\s*·\s*\S|\b(BUY \d+ FOR|SAVE)\b", re.IGNORECASE)
# A shop tool's first line names the store that answered, checked against the shop's own store id (F34).
STORE_FOR_AREA = re.compile(r"^(?:Products|Specials \(multi-buys\)|Promotions) at [^(]+\(the store serving ")
STORE_DEFAULT = re.compile(r"^(?:Products|Specials \(multi-buys\)|Promotions) at [^(]+\(its default store")
NO_CATALOGUE = re.compile(r"No shop near you has a catalogue|Geen winkel naby jou het nog 'n katalogus", re.IGNORECASE)
# A price per kg says so (R99,99/kg, R99,99 per kg); a pack sold by weight costs a range (R87,99–R103,99, R87,99 - 103,99).
PER_KG = re.compile(r"(/\s*kg\b|\bper\s*(kg|kilo))", re.IGNORECASE)
RANGE = re.compile(r"\bR\s?\d[\d\s,.]*?\s*[–-]\s*R?\s?\d")
# A product sold by weight: the shop's link ends in its unit, KG (Checkers and Shoprite: /product/…-10732975KG).
BY_WEIGHT_LINK = re.compile(r"-\d+KG/?$")


def _tables(answer):
    """The answer's markdown tables, each its lines in order: runs of lines that start with '|'."""
    tables, current = [], []
    for line in answer.splitlines():
        if line.lstrip().startswith("|"):
            current.append(line.strip())
        elif current:
            tables.append(current)
            current = []
    if current:
        tables.append(current)
    return tables


def rand_amounts(answer):
    """How many amounts in rand an answer gives."""
    return len(RAND.findall(answer or ""))


def unconfirmed_marks(answer):
    """How often an answer says a value is unknown, estimated or from the search index."""
    return len(UNCONFIRMED.findall(answer or ""))


def priced(answer):
    """A list's ingredients as (priced, listed, guessed, per_kg). A row is priced only with the link its price came
    from (the cook's report links each price to its page): a rand amount without one is a guess from memory, counted
    apart and never as priced (Louis, F31 step 11b: "the idea is to get an actual answer"). The link must be to the shop whose
    column the price is in, or to a shop under a column that names none, so a recipe's link or another shop's is no
    source (review of PR #55); a reference-style link (`[R12,99][1]`) is its URL. An estimate mark or ± is neither. The
    cook's own *Geprys: n van m* line is its claim, so the rows decide; four Nones without a list's rows. Only
    the shopping list counts: a table headed *Bestanddeel* (or Ingredient, Item, Produk), without its bold total rows;
    the branches and what's unsure are tables too (seen in 11b's run). A price per kg is no pack's price (review of
    PR #58: run fc7f8ecfdc87 priced a 1–1,2 kg pack of thighs at R99,99/kg): a cell that says per kg, or one amount
    linked to a product sold by weight, whose pack costs a range, counts apart as per kg, neither priced nor guessed.
    A price whose row links its product at that price's shop is sourced by that link (F34, run 26527259bf31: Puls alone
    linked `[Onions 1kg](checkers…)` and wrote `R34.99` beside it)."""
    answer = answer or ""
    references = {label.strip().lower(): url for label, url in REFERENCE.findall(answer)}
    listed = priced_rows = guessed = per_kg = 0
    for table in _tables(answer):
        if not HEADER.match(table[0]):
            continue
        shops = [_shop_of(name) for name in _cells(table[0])]
        for row in table[1:]:
            if SEPARATOR.match(row) or TOTAL.match(row):
                continue
            listed += 1
            cells = _cells(row)
            # The first column is the ingredient; a price is sourced in its own shop's column, by its own link or by the
            # row's link to the product at that shop.
            linked = " ".join(f"[]({url})" for cell in cells for url in _urls(cell, references))
            sourced = [cell if _sourced(cell, shop, references) else cell + " " + linked
                       for i, cell in enumerate(cells) if i > 0 and RAND.search(cell)
                       for shop in [shops[i] if i < len(shops) else None]
                       if _sourced(cell, shop, references) or _sourced(linked, shop, references)]
            if any(not _per_kg(cell, references) for cell in sourced):
                priced_rows += 1
            elif sourced:
                per_kg += 1
            elif RAND.search(row) and not ESTIMATE.search(row) and "±" not in row:
                guessed += 1
    if not listed:
        return None, None, None, None
    return priced_rows, listed, guessed, per_kg


def _list_rows(answer):
    """The shopping list's tables, each as (its header's cells, its ingredient rows' cells): totals and separators left out."""
    for table in _tables(answer or ""):
        if HEADER.match(table[0]):
            yield _cells(table[0]), [_cells(row) for row in table[1:] if not SEPARATOR.match(row) and not TOTAL.match(row)]


def deals_named(answer):
    """How many of the list's rows name a deal beside a linked price (F34: `· BUY 2 FOR R45`, `SAVE`)."""
    return sum(1 for _, rows in _list_rows(answer) for cells in rows
               if any(INLINE_LINK.search(cell) and DEAL.search(cell) for cell in cells[1:]))


def untooled_shops(answer):
    """How many shops the list prices at that the cook has no tool for (F34, Louis: only shops with a tool): 0 is right."""
    shops = {_shop_of(name) for header, _ in _list_rows(answer) for name in header}
    return len({shop for shop in shops if shop and shop not in TOOL_SHOP_HOSTS})


def store_for_area(results):
    """How many shop searches the store serving the user's town answered (Louis: "that the address was used")."""
    return sum(1 for result in results if STORE_FOR_AREA.search(result or ""))


def store_default(results):
    """How many shop searches the shop's default store answered: no town, none served, or the town's not used."""
    return sum(1 for result in results if STORE_DEFAULT.search(result or ""))


def no_catalogue_line(answer):
    """Whether the answer says no shop near the user has a catalogue here yet (F34: the list without prices)."""
    return bool(NO_CATALOGUE.search(answer or ""))


def _cells(row):
    """A table row's cells, without the outer pipes."""
    return [c.strip() for c in row.strip().strip("|").split("|")]


def _shop_of(header):
    """The site of the shop a column names (Checkers → checkers.co.za), or None for a column that names no shop."""
    name = header.lower()
    return next((host for shop, host in SHOP_HOSTS.items() if shop in name), None)


def _is_host(url, site):
    """Whether a URL is on the site or one of its subdomains (shop.woolworths.co.za is Woolworths; notcheckers isn't)."""
    host = (urlparse(url).hostname or "").lower()
    return host == site or host.endswith("." + site)


def _urls(cell, references):
    """The links in a cell: inline ones, and reference-style ones by their label."""
    urls = INLINE_LINK.findall(cell)
    urls += [references.get((ref or text).strip().lower(), "") for text, ref in REFERENCE_LINK.findall(cell)]
    return [url for url in urls if url]


def _per_kg(cell, references):
    """Whether a sourced price is a price per kg: it says so, or it's one amount for a product sold by weight."""
    if PER_KG.search(INLINE_LINK.sub("]", cell)):   # the words, not the link (a shop's address may hold /kg)
        return True
    return not RANGE.search(cell) and any(BY_WEIGHT_LINK.search(urlparse(url).path) for url in _urls(cell, references))


def _sourced(cell, shop, references):
    """Whether a price cell links to its source: the column's shop, or any shop under a column that names none."""
    sites = [shop] if shop else list(SHOP_HOSTS.values())
    return any(_is_host(url, site) for url in _urls(cell, references) for site in sites)
