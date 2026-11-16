// The tilt (FX162, the design's cards): a panel that says data-tilt leans a little towards the pointer while it is
// over it, as far as the theme's --web-tilt allows, 0 for a theme whose panes stay still. A wide panel leans less, in
// step with its width (Louis: "this panel should also have the tilt option"), and every panel stays still for a
// visitor who asked for less motion.

const WIDE = 420;   // a panel wider than this, in pixels, leans less, in step with its width, so it never swings
let listening = false;

const most = () => +getComputedStyle(document.documentElement).getPropertyValue("--web-tilt") || 0;
const still = () => matchMedia("(prefers-reduced-motion: reduce)").matches;

// Where the pointer is on the panel sets its two angles: across its width it turns, up and down it tips.
const moved = e => {
    const el = e.target instanceof Element ? e.target.closest("[data-tilt]") : null; if (!el) return;
    const r = el.getBoundingClientRect(), deg = most() * Math.min(1, WIDE / (r.width || 1)); if (!deg || still()) return;
    el.style.setProperty("--tilt-ry", `${((e.clientX - r.left) / r.width - .5) * deg}deg`);
    el.style.setProperty("--tilt-rx", `${(.5 - (e.clientY - r.top) / r.height) * deg}deg`);
};
// The pointer leaves: the panel settles flat again.
const left = e => {
    const el = e.target instanceof Element ? e.target.closest("[data-tilt]") : null; if (!el) return;
    if (e.relatedTarget instanceof Node && el.contains(e.relatedTarget)) return;
    el.style.removeProperty("--tilt-rx"); el.style.removeProperty("--tilt-ry");
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
