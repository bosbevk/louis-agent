// Where the agent's caption stands (F134.4, a module beside its component, ADR 0093). The widget's script marks the
// place the agent last lit (data-lit). On a wide screen the caption stands beside that place: to its right where the
// screen has room, else under it, and over the bar again when neither has room or the place is off the screen. It
// follows the page while the visitor scrolls. On a phone it stays over the bar and nothing is moved.
const wide = "(min-width: 901px)";
// The room between the place and the caption, and the least room kept to the screen's edges, in pixels.
const gap = 16;
const edge = 12;
// The site's header, which the caption never stands under, as the widget's script keeps it free.
const headerRoom = 96;
const following = new WeakMap();

// Stands the caption beside the lit place, and keeps it there while the page scrolls or the screen changes size.
export function standBeside(caption) {
    if (!matchMedia(wide).matches) return;
    const follow = () => place(caption);
    following.set(caption, follow);
    addEventListener("scroll", follow, { passive: true });
    addEventListener("resize", follow);
    follow();
}

// The caption is going: it stops following the page.
export function letGo(caption) {
    const follow = following.get(caption);
    if (!follow) return;
    removeEventListener("scroll", follow);
    removeEventListener("resize", follow);
    following.delete(caption);
}

// Where the caption stands now. The bar is fixed and moved by a transform, so it holds the caption's position: the
// place's box is said from the bar's own corner. The caption takes its beside width before it is measured.
const place = (caption) => {
    const bar = caption.closest(".agent-bar");
    const lit = document.querySelector("[data-lit]");
    const box = lit?.getBoundingClientRect();
    const floor = (bar?.querySelector(".agent-row")?.getBoundingClientRect().top ?? innerHeight) - gap;
    caption.classList.add("is-beside");
    const width = caption.offsetWidth, height = caption.offsetHeight;
    const spot = box && box.bottom > headerRoom && box.top < floor ? spotFor(box, width, height, floor) : null;
    if (!bar || !spot) {
        caption.classList.remove("is-beside");
        return;
    }
    const from = bar.getBoundingClientRect();
    caption.style.setProperty("--caption-x", `${spot.x - from.left}px`);
    caption.style.setProperty("--caption-y", `${spot.y - from.top}px`);
};

// To the right of the place where the screen has room, else under it, kept between the header and the bar; none when
// neither has room.
const spotFor = (box, width, height, floor) => {
    const clampY = (y) => Math.max(headerRoom, Math.min(y, floor - height));
    if (innerWidth - box.right - gap - edge >= width) return { x: box.right + gap, y: clampY(box.top) };
    if (floor - box.bottom - gap >= height) {
        return { x: Math.max(edge, Math.min(box.left, innerWidth - width - edge)), y: box.bottom + gap };
    }
    return null;
};
