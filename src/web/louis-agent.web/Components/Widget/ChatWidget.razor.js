// What the visitor is reading (F115, a module beside its component, ADR 0093): the page's address, its title and
// the visible text of its main part, whitespace folded, cut at six thousand characters, so the guide in the corner
// can answer about the page in front of the visitor. Read when a message is sent, so the page is the one rendered.
export function pageText() {
    const main = document.querySelector("main") ?? document.body;
    const text = (main?.innerText ?? "").replace(/\s+/g, " ").trim().slice(0, 6000);
    // The page's named areas (F117): every section with an id, with the heading it carries (its kicker, else its h2),
    // and every card the guide can open (F118), by its title and marked as a card.
    const fold = (s) => (s ?? "").replace(/\s+/g, " ").trim();
    const areas = [...document.querySelectorAll("section[id], [data-card][id]")].map((area) => ({
        id: area.id,
        heading: area.matches("[data-card]")
            ? `${fold(area.querySelector(".card-title")?.textContent)} (card, opens)`
            : fold(area.querySelector(".kicker, h1, h2, h3")?.textContent),
    }));
    return { url: location.pathname, title: document.title, text, areas };
}

// Takes the visitor to an area the guide named (F117, F118): waits for the page to have it (the router draws the new
// page a moment after the address changes), scrolls it to just under the header, and puts a spotlight on it: one
// element laid over the area, which the web's styles draw as a dim over the rest of the page and a ring around the
// area, in and out over four seconds. The spotlight follows the area while it plays, so a page still settling (an
// image loading, a block revealing) keeps it in place. A card is opened as a visitor's click would open it, and the
// light falls on its whole deck, so the card and its story are lit together (F118). Gives up quietly after two
// seconds if the area never appears.
const headerRoom = 96;
const spotlightPad = 14;
const spotlightMs = 4000;
export function showArea(id) {
    const started = performance.now();
    const attempt = () => {
        const area = document.getElementById(id);
        if (area) {
            const card = area.matches("[data-card]");
            if (card && area.getAttribute("aria-expanded") !== "true") area.click();
            const target = card ? area.closest(".card-deck") ?? area : area;
            turnTo(target);
            holdSnap();
            place(target, true);
            // A new page's router scrolls to the top a moment after the area appears, and a card's story grows the
            // deck as it slides in: look again twice, and move once more if the area is not where it should be.
            setTimeout(() => place(target, false), 350);
            setTimeout(() => place(target, false), 900);
            spotlight(target);
            return;
        }
        if (performance.now() - started < 2000) requestAnimationFrame(attempt);
    };
    attempt();
}

// An area inside a band paged sideways (F118) may stand on a later page: the band's body turns to that page first.
const turnTo = (target) => {
    const body = target.closest(".is-paged .band-body");
    if (!body) return;
    const step = body.clientWidth + (parseFloat(getComputedStyle(body).columnGap) || 0);
    const at = body.scrollLeft + target.getBoundingClientRect().left - body.getBoundingClientRect().left;
    body.scrollTo({ left: Math.max(0, Math.floor(at / step) * step), behavior: "auto" });
};

// Where the area belongs (F118, Louis: "the agent must also center it, it takes you to the top"): centred in the
// screen under the header when it fits, else its top just under the header.
const placeFor = (target) => {
    const r = target.getBoundingClientRect(), room = innerHeight - headerRoom;
    const offset = r.height <= room ? headerRoom + (room - r.height) / 2 : headerRoom;
    return Math.max(0, scrollY + r.top - offset);
};

// Moves the page there; a second look moves it only when it is more than a little off.
const place = (target, first) => {
    const top = placeFor(target);
    if (!first && Math.abs(top - scrollY) < 24) return;
    scrollTo({ top, behavior: matchMedia("(prefers-reduced-motion: reduce)").matches ? "auto" : "smooth" });
};

// The page's section snapping would pull the scroll to a section's start, away from the centred area: it rests while
// the spotlight plays and comes back after.
let snapHeld = null;
const holdSnap = () => {
    const root = document.documentElement;
    clearTimeout(snapHeld);
    root.style.scrollSnapType = "none";
    snapHeld = setTimeout(() => root.style.removeProperty("scroll-snap-type"), spotlightMs);
};

// One spotlight at a time: a new one takes the old one down first.
let lit = null;
const spotlight = (area) => {
    lit?.remove();
    const light = document.createElement("div");
    light.className = "area-spotlight";
    light.setAttribute("aria-hidden", "true");
    document.body.append(light);
    lit = light;
    const until = performance.now() + spotlightMs;
    // Lays the light over the area, a little wider than it, in page coordinates, until its time is up.
    const follow = () => {
        if (lit !== light) return;
        const r = area.getBoundingClientRect();
        light.style.top = `${scrollY + r.top - spotlightPad}px`;
        light.style.left = `${scrollX + r.left - spotlightPad}px`;
        light.style.width = `${r.width + 2 * spotlightPad}px`;
        light.style.height = `${r.height + 2 * spotlightPad}px`;
        if (performance.now() < until) requestAnimationFrame(follow);
        else { light.remove(); lit = null; }
    };
    follow();
};
