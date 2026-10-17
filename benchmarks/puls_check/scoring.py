"""Scoring shared by the checks (F31): deterministic code over a transcript, never a model, so a score is the same on
every rerun of the same answer. What a check scores is its own (benchmarks/{check}/); these are the parts more than
one needs. A price checked against its page goes through VerifyQuote with its product (FX16): a price alone matches
any product at that price."""
import re

RAND = re.compile(r"\bR\s?\d[\d\s,.]*")
ESTIMATE = re.compile(r"\b(unknown|onbekend|geskat|skatting|skattings|estimate[ds]?|nie bevestig|unverified|onbevestig|nie gevind)\b", re.IGNORECASE)
UNCONFIRMED = re.compile(r"\b(unknown|onbekend|geskat|skatting|skattings|estimate[ds]?|nie bevestig|unverified|onbevestig|snippets?|soekresultate|soekindeks)\b", re.IGNORECASE)
LINK = re.compile(r"https?://")
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
    never as priced (Louis, F31 step 11b: "the idea is to get an actual answer"). An estimate mark or ± is neither. The
    cook's own *Geprys: n van m* line is its claim, so the rows decide; (None, None, None) without a list's rows. Only
    the shopping list counts: a table headed *Bestanddeel* (or Ingredient, Item, Produk), without its bold total rows;
    the branches and what's unsure are tables too (seen in 11b's run)."""
    rows = [r for table in _tables(answer or "") if HEADER.match(table[0])
            for r in table[1:] if not SEPARATOR.match(r) and not TOTAL.match(r)]
    if not rows:
        return None, None, None
    quoted = [r for r in rows if RAND.search(r) and not ESTIMATE.search(r) and "±" not in r]
    linked = [r for r in quoted if LINK.search(r)]
    return len(linked), len(rows), len(quoted) - len(linked)
