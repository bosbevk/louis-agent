// What a deck's styles can't do (F134.1, a module beside its component, ADR 0093). On a narrow screen the styles lay
// the items out as a row the visitor swipes, and the browser does the swiping. When a swipe comes to rest this module
// tells the deck which item stands in the middle, and when the page puts another item forward it brings the row to
// that one. On a wide screen the items stand in depth and nothing scrolls, so nothing is told and bringing moves
// nothing. There the module leans the items a little towards the mouse. The listeners are on the deck and its own
// row, so they go with it.
import { motion, reducedMotion } from "../../../js/preferences.js";

// How long the row must stand still before a swipe counts as come to rest, in milliseconds.
const rest = 120;
// How far the items lean at most, in degrees: sideways, and up or down. They reach it with the mouse at an edge.
const sideways = 7;
const upOrDown = 5;
const watched = new WeakMap();

const still = () => reducedMotion() && !motion();
const rowOf = (deck) => deck.querySelector(".deck-items");
const itemsOf = (row) => [...row.querySelectorAll(":scope > .deck-item")];
const middleOf = (box) => box.left + box.width / 2;
// How far an item's middle is from the row's middle, to the right as a positive number.
const offOf = (row, item) => middleOf(item.getBoundingClientRect()) - middleOf(row.getBoundingClientRect());

// The item standing nearest the row's middle, by its place in the deck's order.
function inView(row) {
    let nearest = -1, least = Infinity;
    itemsOf(row).forEach((item, index) => {
        const off = Math.abs(offOf(row, item));
        if (off < least) { least = off; nearest = index; }
    });
    return nearest;
}

// Scrolls the row until this item stands in its middle. A row that doesn't scroll stays as it is. It names where to
// end (scrollTo), never how far to go (scrollBy): the row stops on every item for a swipe, and the browser treats a
// "how far" as a swipe too, so it would stop one item on.
function place(row, index, behavior) {
    const item = itemsOf(row)[index];
    if (item) row.scrollTo({ left: row.scrollLeft + offOf(row, item), behavior });
}

// Leans the items towards where the mouse is over the deck: straight with the mouse at the middle, the furthest at
// an edge. It writes two numbers on the deck, and the styles turn the items by them. A finger is not followed, since
// a touch is a swipe or a press, and a visitor who asked for less motion gets none.
function lean(deck, event) {
    if (still() || event.pointerType !== "mouse") return;
    const box = deck.getBoundingClientRect();
    deck.style.setProperty("--deck-lean-y", `${((event.clientX - box.left) / box.width - .5) * sideways}deg`);
    deck.style.setProperty("--deck-lean-x", `${(.5 - (event.clientY - box.top) / box.height) * upOrDown}deg`);
}

// The mouse left the deck: the two numbers go, and the items stand straight again.
function straighten(deck) {
    deck.style.removeProperty("--deck-lean-y");
    deck.style.removeProperty("--deck-lean-x");
}

// Starts watching a deck, with this item forward. Its row is put at the forward item at once, and again whenever
// its size changes: the phone was turned, or a window was made narrow enough for the deck to become a row. And the
// mouse over the deck is followed, for the lean.
export function watch(deck, page, index) {
    const row = rowOf(deck);
    const state = { at: index, timer: 0 };
    const rested = () => {
        const now = inView(row);
        if (!row.isConnected || now < 0 || now === state.at) return;
        state.at = now;
        page.invokeMethodAsync("Viewed", now);
    };
    row.addEventListener("scroll", () => {
        clearTimeout(state.timer);
        state.timer = setTimeout(rested, rest);
    }, { passive: true });
    new ResizeObserver(() => place(row, state.at, "auto")).observe(row);
    deck.addEventListener("pointermove", (event) => lean(deck, event), { passive: true });
    deck.addEventListener("pointerleave", () => straighten(deck));
    watched.set(deck, state);
}

// The page put another item forward: the row goes to it, smoothly unless the visitor asked for less motion.
export function bring(deck, index) {
    const state = watched.get(deck);
    if (!state) return;
    state.at = index;
    place(rowOf(deck), index, still() ? "auto" : "smooth");
}
