// A piece the visitor selects for the guide (F127.7, a module beside its component, ADR 0093): a click on a piece of
// the page ([data-piece][id]), not on a link, a button or a field inside it, marks that piece alone selected
// (data-selected, which the piece's own styles ring) and tells the widget its id and heading. The mark comes off when
// the chip is closed.
let listener = null;

const fold = (s) => (s ?? "").replace(/\s+/g, " ").trim();
const clearMarks = () => document.querySelectorAll("[data-piece][data-selected]")
    .forEach((piece) => piece.removeAttribute("data-selected"));

export function watch(widget) {
    unwatch();
    listener = (event) => {
        if (event.target.closest("a, button, input, textarea, select, label, summary")) return;
        const piece = event.target.closest("[data-piece][id]");
        if (!piece) return;
        clearMarks();
        piece.setAttribute("data-selected", "");
        const heading = fold(piece.querySelector("h1, h2, h3, h4, .card-title")?.textContent) || piece.id;
        widget.invokeMethodAsync("Pick", piece.id, heading);
    };
    document.addEventListener("click", listener);
}

export function clear() {
    clearMarks();
}

export function unwatch() {
    if (listener) document.removeEventListener("click", listener);
    listener = null;
}
