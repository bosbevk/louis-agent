// How the agent's caption comes and goes (F154, Louis's pick A of three; a module of the framework's, ADR 0093, its
// own since standing the caption is its other module's job). It grows out of where it comes from, as a container
// transform: out of the bar's orb, or, when it stands in a card's place, out of that card. It fades a little lower
// before it is taken away, in less time than it took to come, since a thing leaving needs less attention than one
// arriving. A visitor who asked for less motion gets none of it. Both moves are added to whatever else moves the card,
// so the standing module's glide still carries it.
const lessMotionQuery = "(prefers-reduced-motion: reduce)";
// How long the caption takes to grow in, and to fade out, in milliseconds.
const growMs = 380;
const leaveMs = 200;
// The web's spring for coming in, a plain ease for going.
const spring = "cubic-bezier(.34, 1.56, .64, 1)";
const goingEase = "cubic-bezier(.4, 0, 1, 1)";
// How far a leaving caption sinks as it fades, in pixels.
const sink = 8;

// Whether the visitor asked for less motion, so the caption shows its words at once and does not move.
export function lessMotion() {
    return matchMedia(lessMotionQuery).matches;
}

// Grows the caption in, a frame after it is drawn, once its standing module has put it where it stands.
export function growIn(caption) {
    if (lessMotion()) return;
    requestAnimationFrame(() => {
        const to = caption.getBoundingClientRect();
        // Standing in a card's place, it swells out of that card, which is where it now is.
        if (caption.dataset.inPlace !== undefined || !to.width) {
            caption.animate([{ opacity: 0, transform: "scale(.96)" }, { opacity: 1, transform: "none" }],
                { duration: growMs, easing: spring, composite: "add" });
            return;
        }
        // Else out of the bar's orb: from the orb's place and size to its own.
        const orb = document.querySelector(".agent-bar .agent-open")?.getBoundingClientRect();
        if (!orb?.width) return;
        const across = orb.left + orb.width / 2 - (to.left + to.width / 2);
        const down = orb.top + orb.height / 2 - (to.top + to.height / 2);
        const wide = orb.width / to.width, high = orb.height / to.height;
        caption.animate(
            [
                { opacity: 0, transform: `translate(${across}px, ${down}px) scale(${wide}, ${high})` },
                { opacity: 1, transform: "none" }
            ],
            { duration: growMs, easing: spring, composite: "add" });
    });
}

// Fades the caption a little lower, and answers once it has gone, so the page can take it away.
export async function leave(caption) {
    if (lessMotion() || !caption?.isConnected) return;
    const fade = caption.animate(
        [{ opacity: 1, transform: "none" }, { opacity: 0, transform: `translateY(${sink}px)` }],
        { duration: leaveMs, easing: goingEase, fill: "forwards", composite: "add" });
    await fade.finished;
}
