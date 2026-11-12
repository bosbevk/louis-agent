// A piece the visitor selects for the guide (F127.7, a module beside its component, ADR 0093): a click on a piece of
// the page ([data-piece][id]), not on a link, a button or a field inside it, marks that piece alone selected
// (data-selected, which the piece's own styles draw pressed) and tells the widget its id and heading, to ask about it.
// A card the visitor opens, and a piece the guide shows ("piece-shown", from the widget's showArea), is pressed and
// told too, but quietly: the visitor is looking at it, so the next message carries it, with nothing asked. The mark
// comes off when the chip is closed.
let listener = null;
let shown = null;

const fold = (s) => (s ?? "").replace(/\s+/g, " ").trim();
const clearMarks = () => document.querySelectorAll("[data-piece][data-selected]")
    .forEach((piece) => piece.removeAttribute("data-selected"));
const headingOf = (piece) =>
    fold(piece.querySelector("h1, h2, h3, h4, .card-title, .stat-value")?.textContent) || piece.id;
const press = (piece) => {
    clearMarks();
    piece.setAttribute("data-selected", "");
    return headingOf(piece);
};

export function watch(widget) {
    unwatch();
    listener = (event) => {
        // A card is a button that opens: opened, it is where the visitor looks (F127.7, Louis: "when a human opens a
        // card, the agent must know where the user is looking on the page"); closed again, it stays selected, as the
        // chip says, until the chip is closed.
        const card = event.target.closest("[data-card][data-piece][id]");
        if (card) {
            if (card.getAttribute("aria-expanded") !== "true") widget.invokeMethodAsync("Shown", card.id, press(card));
            return;
        }
        if (event.target.closest("a, button, input, textarea, select, label, summary")) return;
        const piece = event.target.closest("[data-piece][id]");
        if (piece) widget.invokeMethodAsync("Pick", piece.id, press(piece));
    };
    shown = (event) => {
        const piece = document.getElementById(event.detail);
        if (piece?.matches("[data-piece]")) widget.invokeMethodAsync("Shown", piece.id, press(piece));
    };
    // Caught on the way down, before the deck's own click turns the card open, so "not yet open" is read as it was.
    document.addEventListener("click", listener, true);
    document.addEventListener("piece-shown", shown);
}

export function clear() {
    clearMarks();
}

export function unwatch() {
    if (listener) document.removeEventListener("click", listener, true);
    if (shown) document.removeEventListener("piece-shown", shown);
    listener = null;
    shown = null;
}
