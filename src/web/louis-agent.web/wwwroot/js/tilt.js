// The tilt (FX162, the design's cards): a panel that says data-tilt leans a little towards the pointer while it is
// over it, as far as the theme's --web-tilt allows, 0 for a theme whose panes stay still. A wide panel stays still,
// as the design's do, and so does every panel for a visitor who asked for less motion.

const WIDE = 700;   // a panel wider than this, in pixels, stays still
let listening = false;

const most = () => +getComputedStyle(document.documentElement).getPropertyValue("--web-tilt") || 0;
const still = () => matchMedia("(prefers-reduced-motion: reduce)").matches;

// Where the pointer is on the panel sets its two angles: across its width it turns, up and down it tips.
const moved = e => {
    const el = e.target instanceof Element ? e.target.closest("[data-tilt]") : null; if (!el) return;
    const deg = most(), r = el.getBoundingClientRect(); if (!deg || still() || r.width > WIDE) return;
    el.style.setProperty("--web-ry", `${((e.clientX - r.left) / r.width - .5) * deg}deg`);
    el.style.setProperty("--web-rx", `${(.5 - (e.clientY - r.top) / r.height) * deg}deg`);
};
// The pointer leaves: the panel settles flat again.
const left = e => {
    const el = e.target instanceof Element ? e.target.closest("[data-tilt]") : null; if (!el) return;
    if (e.relatedTarget instanceof Node && el.contains(e.relatedTarget)) return;
    el.style.removeProperty("--web-rx"); el.style.removeProperty("--web-ry");
};

// Hangs the two listeners once; safe to call after every render.
export const tilt = {
    attach() {
        if (listening) return;
        listening = true;
        document.addEventListener("pointermove", moved, { passive: true });
        document.addEventListener("pointerout", left, { passive: true });
    },
    detach() {
        if (!listening) return;
        listening = false;
        document.removeEventListener("pointermove", moved);
        document.removeEventListener("pointerout", left);
    }
};
