"""Scoring shared by the checks (F31): deterministic code over a transcript, never a model, so a score is the same on
every rerun of the same answer. What a check scores is its own (benchmarks/{check}/); these are the parts more than
one needs. A price checked against its page goes through VerifyQuote with its product (FX16): a price alone matches
any product at that price."""
import re

RAND = re.compile(r"\bR\s?\d[\d\s,.]*")
# The cook's own tally, a fixed line at the end of its shopping list: "Geprys: 7 van 9 bestanddele".
PRICED = re.compile(r"\b(?:Geprys|Priced)\s*:\s*(\d+)\s+(?:van|of)\s+(\d+)", re.IGNORECASE)
ESTIMATE = re.compile(r"\b(unknown|onbekend|geskat|skatting|skattings|estimate[ds]?|nie bevestig|unverified|onbevestig|nie gevind)\b", re.IGNORECASE)
UNCONFIRMED = re.compile(r"\b(unknown|onbekend|geskat|skatting|skattings|estimate[ds]?|nie bevestig|unverified|onbevestig|snippets?|soekresultate|soekindeks)\b", re.IGNORECASE)
HEADER = re.compile(r"^\|\s*(Item|Bestanddeel|Ingredient|Produk)\b", re.IGNORECASE)


def rand_amounts(answer):
    """How many amounts in rand an answer gives."""
    return len(RAND.findall(answer or ""))


def unconfirmed_marks(answer):
    """How often an answer says a value is unknown, estimated or from the search index."""
    return len(UNCONFIRMED.findall(answer or ""))


def priced(answer):
    """Ingredients with a price over ingredients on the list: the cook's own tally line when it wrote one, else the
    list's table rows, a row with a rand amount and no estimate mark being priced. (None, None) without a list."""
    if m := PRICED.search(answer or ""):
        n, total = int(m.group(1)), int(m.group(2))
        return (min(n, total), total) if total else (None, None)
    rows = [r for r in re.findall(r"^\|(?!\s*-)[^\n]*$", answer or "", re.MULTILINE) if not HEADER.match(r)]
    if not rows:
        return None, None
    good = [r for r in rows if RAND.search(r) and not ESTIMATE.search(r) and "±" not in r]
    return len(good), len(rows)
