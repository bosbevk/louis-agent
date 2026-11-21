// The room the agent's bar takes at the page's foot (F134.3, a module beside its component, ADR 0093). The bar is
// fixed over the page, so the page can't see how tall it stands. This module tells it: from the top of the bar, with
// whatever stands over its row, down to the screen's foot, written on the page as --bar-room. The frame that draws
// the bar keeps that room free, so no page ends under the bar (F134's rule 5). It is told again whenever the bar's
// height or the screen's changes.
//
// The room never drops under the resting bar's, its chips and all. The chips step away each time the agent answers,
// and a page that took their room back would shift under the visitor every time.

// A little air between the page's last thing and the bar, and the bar's own gap over its row, in pixels.
const air = 20;
const gap = 10;
// The most of the screen's height the bar may claim, so a long answer over it never takes the page away.
const most = 0.45;
const kept = new WeakMap();

// Keeps the page told how much room the bar takes.
export function keepRoom(bar) {
    const tell = () => {
        const room = Math.min(roomOf(bar), innerHeight * most);
        document.documentElement.style.setProperty("--bar-room", `${Math.round(room)}px`);
    };
    const sizes = new ResizeObserver(tell);
    sizes.observe(bar);
    addEventListener("resize", tell);
    kept.set(bar, { tell, sizes });
    tell();
}

// The bar is going: the page gets its room back.
export function letGo(bar) {
    const keeping = kept.get(bar);
    if (!keeping) return;
    keeping.sizes.disconnect();
    removeEventListener("resize", keeping.tell);
    document.documentElement.style.removeProperty("--bar-room");
    kept.delete(bar);
}

// The room the bar needs now: as it stands, or as it rests with its chips, whichever is more.
const roomOf = (bar) => {
    const standing = innerHeight - bar.getBoundingClientRect().top + air;
    return Math.max(standing, restingRoomOf(bar));
};

// The room the resting bar takes with its chips. While the bar is closed on a wide screen the chips stand round the
// orb, placed apart from the bar's flow and never folded, so the room starts at their own top or the row's, whichever
// is higher. Otherwise they stand over the row, and their height is read from what they hold, since stepped away
// they are folded to none.
const restingRoomOf = (bar) => {
    const rowTop = (bar.querySelector(".agent-row") ?? bar).getBoundingClientRect().top;
    const chips = bar.querySelector(".agent-chips");
    if (!chips) return innerHeight - rowTop + air;
    if (getComputedStyle(chips).position === "absolute")
        return innerHeight - Math.min(rowTop, chips.getBoundingClientRect().top) + air;
    return innerHeight - rowTop + chips.scrollHeight + gap + air;
};
