// A piece the visitor selects for the guide (F127.7, a module beside its component, ADR 0093): a click on a piece of
// the page ([data-piece][id]), not on a link, a button or a field inside it, marks that piece alone selected
// (data-selected, which the piece's own styles draw pressed) and tells the widget its id and heading, to ask about it.
// A card the visitor opens, and a piece the guide shows ("piece-shown", from the widget's showArea), is pressed and
// told too, but quietly: the visitor is looking at it, so the next message carries it, with nothing asked. The mark
// comes off when the chip is closed.
let listener = null;
let shown = null;
let near = null;
let lit = null;
let words = null;

// The design's tag on a piece (FX163): one small label in its corner holding the three words, the piece's state
// choosing which one shows, "ask" under the pointer, "picked" once selected, "shown" while the agent shows it.
const tagged = (piece) => {
    if (!words || !piece || piece.querySelector(":scope > .piece-tag")) return;
    const tag = document.createElement("i");
    tag.className = "piece-tag";
    tag.setAttribute("aria-hidden", "true");
    tag.dataset.ask = words.ask;
    tag.dataset.picked = words.picked;
    tag.dataset.shown = words.shown;
    piece.append(tag);
};

const fold = (s) => (s ?? "").replace(/\s+/g, " ").trim();
const clearMarks = () => document.querySelectorAll("[data-piece][data-selected]")
    .forEach((piece) => piece.removeAttribute("data-selected"));
const headingOf = (piece) =>
    fold(piece.querySelector("h1, h2, h3, h4, .card-title, .stat-value")?.textContent) || piece.id;
const press = (piece) => {
    clearMarks();
    tagged(piece);
    piece.setAttribute("data-selected", "");
    return headingOf(piece);
};

export function watch(widget, tags) {
    unwatch();
    words = tags;
    // A piece is tagged as the pointer first comes near it, and the place the agent lights is tagged as it is lit.
    near = (event) => tagged(event.target instanceof Element ? event.target.closest("[data-piece][id]") : null);
    lit = () => tagged(document.querySelector("[data-piece][data-lit]"));
    document.addEventListener("pointerover", near, { passive: true });
    document.addEventListener("place-lit", lit);
    listener = (event) => {
        // A card is a button that opens: opened, it is where the visitor looks (F127.7, Louis: "when a human opens a
        // card, the agent must know where the user is looking on the page"); closed again, it stays selected, as the
        // chip says, until the chip is closed.
        const card = event.target.closest("[data-card][data-piece][id]");
        if (card) {
            if (card.getAttribute("aria-expanded") !== "true") widget.invokeMethodAsync("Shown", card.id, press(card));
            return;
        }
        // A click in the guide's own bar keeps the pick: that is where the visitor asks about it.
        if (event.target.closest(".chat-widget")) return;
        const piece = event.target.closest("[data-piece][id]");
        const picked = document.querySelector("[data-piece][data-selected]");
        // A click away from the pieces, or on the picked one again, lets the pick go and the page is as it was.
        if (picked && (!piece || piece === picked)) {
            clearMarks();
            widget.invokeMethodAsync("LetGo");
            return;
        }
        // A link or a field inside a piece keeps its own click; a piece that is itself a link, such as a photograph of a
        // demo, is picked instead of followed (FX163, Louis: "each of these 3 also ask about this").
        const control = event.target.closest("a, button, input, textarea, select, label, summary");
        if (control && control !== piece) return;
        if (!piece) return;
        event.preventDefault();
        widget.invokeMethodAsync("Pick", piece.id, press(piece));
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
    if (near) document.removeEventListener("pointerover", near);
    if (lit) document.removeEventListener("place-lit", lit);
    listener = null;
    shown = null;
    near = null;
    lit = null;
}
