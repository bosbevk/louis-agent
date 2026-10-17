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
    """A list's ingredients as (priced, listed, guessed). A row is priced only with the link its price came from (the
    cook's report links each price to its page): a rand amount without one is a guess from memory, counted apart and
    never as priced (Louis, F31 step 11b: "the idea is to get an actual answer"). The link must be to the shop whose
    column the price is in, or to a shop under a column that names none, so a recipe's link or another shop's is no
    source (review of PR #55); a reference-style link (`[R12,99][1]`) is its URL. An estimate mark or ± is neither. The
    cook's own *Geprys: n van m* line is its claim, so the rows decide; (None, None, None) without a list's rows. Only
    the shopping list counts: a table headed *Bestanddeel* (or Ingredient, Item, Produk), without its bold total rows;
    the branches and what's unsure are tables too (seen in 11b's run)."""
    answer = answer or ""
    references = {label.strip().lower(): url for label, url in REFERENCE.findall(answer)}
    listed = priced_rows = guessed = 0
    for table in _tables(answer):
        if not HEADER.match(table[0]):
            continue
        shops = [_shop_of(name) for name in _cells(table[0])]
        for row in table[1:]:
            if SEPARATOR.match(row) or TOTAL.match(row):
                continue
            listed += 1
            cells = _cells(row)
            # The first column is the ingredient; a price is sourced in its own shop's column.
            if any(RAND.search(cell) and _sourced(cell, shops[i] if i < len(shops) else None, references)
                   for i, cell in enumerate(cells) if i > 0):
                priced_rows += 1
            elif RAND.search(row) and not ESTIMATE.search(row) and "±" not in row:
                guessed += 1
    if not listed:
        return None, None, None
    return priced_rows, listed, guessed


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


def _sourced(cell, shop, references):
    """Whether a price cell links to its source: the column's shop, or any shop under a column that names none."""
    urls = INLINE_LINK.findall(cell)
    urls += [references.get((ref or text).strip().lower(), "") for text, ref in REFERENCE_LINK.findall(cell)]
    sites = [shop] if shop else list(SHOP_HOSTS.values())
    return any(_is_host(url, site) for url in urls if url for site in sites)
