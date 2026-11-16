// Where the agent's caption stands (F134.4, a module beside its component, ADR 0093). The widget's script marks the
// place the agent last lit (data-lit) and says when it lights another. On a wide screen a caption whose answer showed
// a place (data-beside) stands beside that place: to its right where the screen has room, else to its left, else
// under it. An answer that lit no place stands the same way beside the piece of the page the visitor chose to ask
// about, when one is chosen. When that place has a card beside it, the next piece of the page or else the one
// before, the caption takes that card's place instead: where it stands, its size and its corners (F151.4). While it
// stands there, the place keeps a quiet ring, so the two read as one. A caption
// with no place to stand beside (none was lit or chosen, the place is as wide as the screen, or it is scrolled off)
// stands beside the bar where the screen has room, and over the bar where it has none. It follows the page while
// the visitor scrolls. On a phone it stays over the bar and nothing is moved.
//
// The module's marks on a caption it has stood are attributes of their own (data-stands, and data-in-place while it
// has a card's place), never a class: the page writes the caption's classes again whenever they change, and would
// wipe a class added here.
import { boxOf } from "../../ChatWidget.razor.js";

const wide = "(min-width: 901px)";
// The room between the place and the caption, and the least room kept to the screen's edges, in pixels.
const gap = 24;
const edge = 16;
// The site's header, which the caption never stands under, as the widget's script keeps it free.
const headerRoom = 96;
// How far the ring stands off the place, and the least it keeps to the screen's sides: the spotlight's own numbers,
// so the ring is the spotlight's, left on.
const ringPad = 14;
const ringEdge = 4;
// How long the caption follows a place frame by frame after it is lit, while the place may still be moving.
const settleMs = 1400;
// How long the caption takes to glide from one spot to another, and how it eases: the web's own ease.
const glideMs = 500;
const glideEase = "cubic-bezier(.2, .8, .2, 1)";
const lessMotion = "(prefers-reduced-motion: reduce)";
const standing = new WeakMap();

// Stands the caption beside the lit place, and keeps it there while the page scrolls, the screen changes size, the
// caption grows with its answer, or the agent lights another place.
export function standBeside(caption) {
    if (!matchMedia(wide).matches) return;
    const ring = document.createElement("div");
    ring.className = "caption-ring";
    ring.setAttribute("aria-hidden", "true");
    document.body.append(ring);
    const follow = () => place(caption, ring);
    const stood = { follow, ring, sizes: new ResizeObserver(follow) };
    // A place lit while the caption already stands: it glides there from where it stood, then follows the place.
    stood.settle = () => {
        glide(caption, follow);
        followForAMoment(stood);
    };
    stood.sizes.observe(caption);
    standing.set(caption, stood);
    addEventListener("scroll", follow, { passive: true });
    addEventListener("resize", follow);
    document.addEventListener("place-lit", stood.settle);
    // A caption just drawn takes its spot at once: its own entry is the only motion it needs.
    followForAMoment(stood);
}

// The caption's answer showed a place after its first words: it is stood again, now beside that place.
export function standAgain(caption) {
    standing.get(caption)?.settle();
}

// The caption is going: it stops following the page, and its ring goes with it. The place it showed is shown no
// more (FX163, Louis: "when I click close or focus away the screen must return to normal like in the design").
export function letGo(caption) {
    for (const marked of document.querySelectorAll("[data-lit]")) delete marked.dataset.lit;
    const stood = standing.get(caption);
    if (!stood) return;
    stood.until = 0;
    removeEventListener("scroll", stood.follow);
    removeEventListener("resize", stood.follow);
    document.removeEventListener("place-lit", stood.settle);
    stood.sizes.disconnect();
    stood.ring.remove();
    standing.delete(caption);
}

// A place just lit may still be moving, as a window coming forward on a deck is, or a page scrolling to it: the
// caption is placed again each frame for a moment, and then rests until something else moves it.
const followForAMoment = (stood) => {
    const following = stood.until > performance.now();
    stood.until = performance.now() + settleMs;
    if (following) return;
    const frame = () => {
        stood.follow();
        if (performance.now() < stood.until) requestAnimationFrame(frame);
    };
    frame();
};

// Moves the caption by what `move` does, and plays the way there from where it stood. The caption takes its new spot
// at once, so it keeps exactly with the page while the page scrolls, and the glide is laid over that as a shift that
// runs out. A visitor who asked for less motion gets the move alone.
const glide = (caption, move) => {
    const from = caption.getBoundingClientRect();
    move();
    const to = caption.getBoundingClientRect();
    const across = from.left - to.left, down = from.top - to.top;
    if (matchMedia(lessMotion).matches || Math.abs(across) + Math.abs(down) < 2) return;
    const way = [{ transform: `translate(${across}px, ${down}px)` }, { transform: "none" }];
    const pace = { duration: glideMs, easing: glideEase };
    caption.animate(way, pace);
};

// Where the caption stands now, and the ring with it: beside the place, else beside the bar, else over the bar as it
// is drawn. The caption takes its standing look before it is measured, and gives it up again when neither has room.
const place = (caption, ring) => {
    // The place the answer lit, when it lit one. Else the piece of the page the visitor chose to ask about, when
    // one is chosen: the answer is about that piece, whether or not the agent lit it again.
    const lit = caption.hasAttribute("data-beside") ? document.querySelector("[data-lit]") : null;
    const about = lit ?? document.querySelector("[data-piece][data-selected]");
    const box = about ? boxOf(about) : null;
    const bar = caption.closest(".agent-bar");
    const row = bar?.querySelector(".agent-row");
    // Marked first, the caption is out of the bar's flow, so the bar's top is the top of what else it holds: its
    // row, and the chip of a selected piece over it. The caption stands clear of both.
    caption.dataset.stands = "";
    const floor = (bar?.getBoundingClientRect().top ?? innerHeight) - gap;
    // A piece in a row of cards: the caption takes the place of the cards beside it, as the row's rule says, and the
    // place keeps its ring (FX163, Louis: "can we configure the AI agent to attach like the following").
    const cards = about ? cardsBeside(about, floor) : [];
    if (cards.length) return takeThePlaceOf(cards, caption, ring, box);
    // A wide piece standing alone, such as a product window, gives the caption its right third, from its top down
    // (Louis: "our agent must take some of the control space, so he must be shrunk like this").
    // Only a piece: a whole section the guide lit is no piece, and stays the page's (it flashed a strip down the right).
    if (about?.matches("[data-piece]") && box && box.width >= WIDE_PIECE && box.bottom > headerRoom && box.top < floor) {
        const width = box.width * WIDE_SHARE;
        return standIn(caption, ring, box, { left: box.right - width, top: box.top, width, height: box.height },
            getComputedStyle(about).borderRadius);
    }
    // Anywhere else, on a site whose answers rise from underneath, the caption stays over its bar and only the place
    // keeps its ring (Louis: "always from underneath in flow layout", "or just make it an option").
    if (document.querySelector(".site.answers-under")) {
        delete caption.dataset.stands;
        delete caption.dataset.inPlace;
        const shown = box && box.width > 0 && box.top + box.height > headerRoom && box.top < floor;
        ring.classList.toggle("is-on", !!shown);
        if (shown) ringAround(ring, box);
        return;
    }
    delete caption.dataset.inPlace;
    const width = caption.offsetWidth, height = caption.offsetHeight;
    const onScreen = box && box.width > 0 && box.top + box.height > headerRoom && box.top < floor;
    const beside = onScreen ? spotFor(box, width, height, floor) : null;
    const spot = beside ?? besideTheBar(row, width, height);
    ring.classList.toggle("is-on", beside !== null);
    if (beside) ringAround(ring, box);
    if (!spot) {
        delete caption.dataset.stands;
        return;
    }
    caption.style.setProperty("--caption-x", `${Math.round(spot.x)}px`);
    caption.style.setProperty("--caption-y", `${Math.round(spot.y)}px`);
};

// Which of a row's cards give way to the answer (FX163, Louis's two pictures): in a row of four, the two on the far
// side, the second and third for the first, the third and fourth for the second, the first and second for the
// third, the second and third for the fourth; in a row of three or fewer, one beside it, the next one, or the one
// before for the last. The row is the piece's own pieces standing side by side.
const ROW_GIVES = { 4: [[1, 2], [2, 3], [0, 1], [1, 2]] };
const givers = (at, count) => ROW_GIVES[count]?.[at] ?? (at + 1 < count ? [at + 1] : [at - 1]);

// The cards beside a place that the caption stands over, when the place is a card in a row of them lying flat (a
// card turned or lifted is on its way somewhere) and showing between the header and the bar. None otherwise.
const cardsBeside = (about, floor) => {
    const row = [...(about.parentElement?.children ?? [])].filter(card => card.matches("[data-piece]"));
    // One row is the cards laid at one height: read from the layout, which a picked card's press or a tilt doesn't
    // move (the picked card's drawn top was a few pixels off, and the row was missed).
    const at = row.indexOf(about), top = about.offsetTop;
    if (row.length < 2 || row.length > 4 || row.some(card => Math.abs(card.offsetTop - top) > 2)) return [];
    const cards = givers(at, row.length).map(i => row[i]).filter(Boolean);
    // A card lies flat and shows, at least partly, between the header and the bar (Louis: "chat should have opened next
    // to it", of a card whose top had passed under the header).
    const lies = card => { const box = card.getBoundingClientRect();
        return getComputedStyle(card).transform === "none" && box.width > 0 && box.bottom > headerRoom && box.top < floor; };
    return cards.length && cards.every(lies) ? cards : [];
};

// A piece this wide, in pixels, standing alone, gives the caption a share of itself; that share is its right third.
const WIDE_PIECE = 700;
const WIDE_SHARE = 1 / 3;

// Stands the caption where the cards are, as wide as all of them and as tall, with their corners, and rings the
// place.
const takeThePlaceOf = (cards, caption, ring, placeBox) => {
    const boxes = cards.map(card => card.getBoundingClientRect());
    const left = Math.min(...boxes.map(b => b.left)), right = Math.max(...boxes.map(b => b.right));
    const top = Math.min(...boxes.map(b => b.top)), bottom = Math.max(...boxes.map(b => b.bottom));
    standIn(caption, ring, placeBox, { left, top, width: right - left, height: bottom - top },
        getComputedStyle(cards[0]).borderRadius);
};

// Stands the caption in a box on the page, its size at least the box's (it grows when its words need more), with the
// corners given, and rings the place.
const standIn = (caption, ring, placeBox, at, corners) => {
    caption.dataset.stands = "";
    caption.dataset.inPlace = "";
    // A box that starts under the header is cut to start below it, so the words never hide behind the header.
    const top = Math.max(at.top, headerRoom), height = at.height - (top - at.top);
    caption.style.setProperty("--caption-x", `${Math.round(at.left)}px`);
    caption.style.setProperty("--caption-y", `${Math.round(top)}px`);
    caption.style.setProperty("--caption-w", `${Math.round(at.width)}px`);
    caption.style.setProperty("--caption-h", `${Math.round(Math.max(height, 0))}px`);
    caption.style.setProperty("--caption-r", corners);
    ring.classList.add("is-on");
    ringAround(ring, placeBox);
};

// To the right of the place where the screen has room, else to its left, else under it, kept between the header and
// the bar; none when no side has room.
const spotFor = (box, width, height, floor) => {
    const right = box.left + box.width, bottom = box.top + box.height;
    const clampY = (y) => Math.max(headerRoom, Math.min(y, floor - height));
    if (innerWidth - right - gap - edge >= width) return { x: right + gap, y: clampY(box.top) };
    if (box.left - gap - edge >= width) return { x: box.left - gap - width, y: clampY(box.top) };
    if (floor - bottom - gap >= height) {
        return { x: Math.max(edge, Math.min(box.left, innerWidth - width - edge)), y: bottom + gap };
    }
    return null;
};

// Beside the bar, at its left, standing on the bar's own foot: where a caption with no place to stand beside goes
// on a screen wide enough, so it covers nothing of the page's middle and the page need make no room for it. The
// left is the orb's side, so the caption reads as the orb speaking, and it keeps the caption clear of what a page
// has at its right: a demo's live window, and the whole talk's drawer. None when the screen has no such room.
const besideTheBar = (row, width, height) => {
    const bar = row?.getBoundingClientRect();
    if (!bar || bar.left - gap - edge < width) return null;
    return { x: bar.left - gap - width, y: Math.max(headerRoom, bar.bottom - height) };
};

// Lays the ring over the place, a little wider, in the page's own coordinates so it moves with the page, and inside
// the screen's width so it gives the page no sideways scroll.
const ringAround = (ring, box) => {
    const screen = document.documentElement.clientWidth;
    const left = Math.max(ringEdge, box.left - ringPad);
    const right = Math.min(screen - ringEdge, box.left + box.width + ringPad);
    ring.style.top = `${scrollY + box.top - ringPad}px`;
    ring.style.left = `${scrollX + left}px`;
    ring.style.width = `${right - left}px`;
    ring.style.height = `${box.height + 2 * ringPad}px`;
};
