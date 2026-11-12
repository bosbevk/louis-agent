// What the visitor is reading (F115, a module beside its component, ADR 0093): the page's address, its title and
// the visible text of its main part, whitespace folded, cut at six thousand characters, so the guide in the corner
// can answer about the page in front of the visitor. Read when a message is sent, so the page is the one rendered.
export function pageText() {
    const main = document.querySelector("main") ?? document.body;
    const text = (main?.innerText ?? "").replace(/\s+/g, " ").trim().slice(0, 6000);
    // The page's named areas (F117): every section with an id, with the heading it carries (its kicker, else its h2),
    // and every card the guide can open (F118), by its title and marked as a card.
    // A paged section's first page is the section itself, so its heading carries "page 1 of 2: ..." and only the pages
    // after it are places of their own; listed apart, a walkthrough stopped twice on the same screen (F118).
    const fold = (s) => (s ?? "").replace(/\s+/g, " ").trim();
    const areas = [...document.querySelectorAll("section[id], [data-card][id]")].flatMap((area) => {
        const heading = area.matches("[data-card]")
            ? `${fold(area.querySelector(".card-title")?.textContent)} (card, opens)`
            : fold(area.querySelector(".kicker, h1, h2, h3")?.textContent);
        const [first, ...later] = pagesOf(area, fold);
        return [{ id: area.id, heading: first ? `${heading} (${first.heading})` : heading }, ...later];
    });
    return { url: location.pathname, title: document.title, text, areas, links: linksOf(main, fold) };
}

// The page's links within the site (FX120), each once, by the address it opens and its words, so the guide can take
// the visitor where a link on their screen leads; a link with no words names its address.
const linksOf = (main, fold) => {
    const seen = new Set();
    return [...main.querySelectorAll("a[href^='/']")].flatMap((a) => {
        const href = a.getAttribute("href");
        if (seen.has(href)) return [];
        seen.add(href);
        return [{ id: href, heading: fold(a.textContent).slice(0, 60) || href }];
    }).slice(0, 40);
};

// The section on the visitor's screen (F118): the last whose top has passed the line just under the header, the same
// rule the section index lights by; at the page's end, the last section. Sent with every message, so "this" is known.
export function inView() {
    const fold = (s) => (s ?? "").replace(/\s+/g, " ").trim();
    const sections = [...document.querySelectorAll(".site-main > section[id]")];
    if (sections.length === 0) return null;
    let at = sections[0];
    for (const section of sections) if (section.getBoundingClientRect().top <= 140) at = section;
    if (scrollY + innerHeight >= document.documentElement.scrollHeight - 2) at = sections[sections.length - 1];
    return { id: at.id, heading: fold(at.querySelector(".kicker, h1, h2, h3")?.textContent) };
}

// A business's window took the guide's ask (F127.4.2): on a narrow screen, where the guide's panel covers the page,
// the window is brought into view (it may still be drawing, so it is looked for a moment) and the answer is true, so
// the widget folds to its head; on a wide screen nothing moves.
export function showWindow(selector) {
    if (!matchMedia("(max-width: 720px)").matches) return false;
    const started = performance.now();
    const attempt = () => {
        const target = document.querySelector(selector);
        if (target) target.scrollIntoView({ block: "center", behavior: "smooth" });
        else if (performance.now() - started < 2000) requestAnimationFrame(attempt);
    };
    attempt();
    return true;
}

// The card open on the visitor's screen (FX106), by its id and title, or null: what they opened is what "this" means,
// not a card the guide opened earlier. A deck opens one card at a time, so the first open one is it.
export function openCard() {
    const fold = (s) => (s ?? "").replace(/\s+/g, " ").trim();
    const card = document.querySelector("[data-card][id][aria-expanded='true']");
    return card ? { id: card.id, heading: fold(card.querySelector(".card-title")?.textContent) } : null;
}

// A section paged sideways on this visitor's screen (F118) lists its pages, each by what it holds, so the guide can
// turn to the page it talks about: "parts/2", "page 2 of 3: Your program, The hosts, ...". The count is the pager's,
// and an item that flows on to the next page (a long list) names that page too, by the first named thing on it, so a
// page that only continues something is never left out of a walkthrough (FX109).
const stepOf = (body) => body.clientWidth + (parseFloat(getComputedStyle(body).columnGap) || 0);
const named = "strong, h3, h4, .card-title, figcaption";
const pagesOf = (area, fold) => {
    const body = area.matches(".is-paged") ? area.querySelector(".band-body") : null;
    if (!body) return [];
    const step = stepOf(body), left = body.getBoundingClientRect().left - body.scrollLeft;
    const pageOf = (rect) => Math.floor((rect.left - left + 1) / step);
    const count = Math.max(1, Math.round((body.scrollWidth + step - body.clientWidth) / step));
    const pages = Array.from({ length: count }, () => []);
    for (const item of body.children) {
        const name = fold(item.querySelector(named)?.textContent ?? item.textContent).slice(0, 40);
        // An item cut across pages has a box on each of them; the first holds its name, a later one the first named
        // thing inside it on that page, else the item's name as continued.
        [...item.getClientRects()].map(pageOf).forEach((page, k) => {
            const inside = k === 0
                ? null
                : [...item.querySelectorAll(named)].find((e) => pageOf(e.getBoundingClientRect()) === page);
            const label = k === 0 ? name : fold(inside?.textContent).slice(0, 40) || (name && `${name} (continued)`);
            if (label && pages[page] && !pages[page].includes(label)) pages[page].push(label);
        });
    }
    return pages.map((names, k) => ({
        id: `${area.id}/${k + 1}`,
        heading: `page ${k + 1} of ${count}: ${names.join(", ") || "continued"}`,
    }));
};

// Takes the visitor to an area the guide named (F117, F118): waits for the page to have it (the router draws the new
// page a moment after the address changes), scrolls it to just under the header, and puts a spotlight on it: one
// element laid over the area, which the web's styles draw as a dim over the rest of the page and a ring around the
// area, in and out over four seconds. The spotlight follows the area while it plays, so a page still settling (an
// image loading, a block revealing) keeps it in place. A card is opened as a visitor's click would open it, and the
// light falls on its whole deck, so the card and its story are lit together (F118). Gives up quietly after two
// seconds if the area never appears.
const headerRoom = 96;
const spotlightPad = 14;
const spotlightEdge = 4;
// How far the ring reaches past each side as it comes in: half of the 4% it grows by (the styles' scale of 1.04).
const spotlightGrow = 0.02;
const spotlightMs = 4000;
export function showArea(name, pageAddress, address) {
    // "parts/2" is the area "parts", turned to its second page.
    const [id, page] = name.split("/");
    const started = performance.now();
    const attempt = () => {
        const area = document.getElementById(id);
        if (area) {
            // The address bar shows the place's own address (ADR 0107), held while the spotlight plays.
            if (address) {
                document.documentElement.dataset.addressHeld = String(Date.now() + spotlightMs);
                history.replaceState(history.state, "", address);
            }
            const card = area.matches("[data-card]");
            if (card && area.getAttribute("aria-expanded") !== "true") area.click();
            const target = card ? area.closest(".card-deck") ?? area : area;
            if (page) turnToPage(area, Number(page));
            else turnTo(target);
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
    const step = stepOf(body);
    const at = body.scrollLeft + target.getBoundingClientRect().left - body.getBoundingClientRect().left;
    body.scrollTo({ left: Math.max(0, Math.floor(at / step) * step), behavior: "auto" });
};

// A paged band named with its page ("parts/2") turns to that page, smoothly, as its pager's button would.
const turnToPage = (band, page) => {
    const body = band.matches(".is-paged") ? band.querySelector(".band-body") : null;
    if (!body || !(page >= 1)) return;
    const still = matchMedia("(prefers-reduced-motion: reduce)").matches;
    body.scrollTo({ left: (page - 1) * stepOf(body), behavior: still ? "auto" : "smooth" });
};

// The box around what a place holds (F118, Louis: "highlight only this section, the section right around the actual
// parts"): a band is a screen tall on a page of slides, mostly room, so its box is its content's, the kicker to the
// last line, and on a page paged sideways its body and pager. Any other section, such as a site's hero, is a screen
// tall too, so its box is what it holds (FX121, Louis: "highlight too big"); a card or a smaller place is its own box.
const boxOf = (target) => {
    if (target.matches(".band")) {
        const paged = target.matches(".is-paged");
        return unionOf(target, paged
            ? [target.querySelector(".band-body"), target.querySelector(".band-pager:not([hidden])")]
            : [...(target.querySelector(".band-body")?.children ?? [])]);
    }
    return target.matches("section") ? unionOf(target, [...target.children]) : target.getBoundingClientRect();
};

// The box around the parts that show; the target's own box when none does.
const unionOf = (target, parts) => {
    const rects = parts.filter(Boolean).map(part => part.getBoundingClientRect())
        .filter(r => r.width > 0 && r.height > 0);
    if (rects.length === 0) return target.getBoundingClientRect();
    const top = Math.min(...rects.map(r => r.top)), bottom = Math.max(...rects.map(r => r.bottom));
    const left = Math.min(...rects.map(r => r.left)), right = Math.max(...rects.map(r => r.right));
    return { top, left, width: right - left, height: bottom - top };
};

// Where the area belongs (F118, Louis: "the agent must also center it, it takes you to the top"): centred in the
// screen under the header when it fits, else its top just under the header.
const placeFor = (target) => {
    const r = boxOf(target), room = innerHeight - headerRoom;
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
    // Lays the light over what the area holds, a little wider, in page coordinates, until its time is up. It keeps
    // inside the screen's width (F127.1): a ring past the edge gives the page a sideways scroll, so a place as wide as
    // the screen (a demo's stage) gets a ring that stops at the edge, and comes in without growing if growing would
    // cross it.
    const follow = () => {
        if (lit !== light) return;
        const r = boxOf(area), screen = document.documentElement.clientWidth;
        const left = Math.max(spotlightEdge, r.left - spotlightPad);
        const right = Math.min(screen - spotlightEdge, r.left + r.width + spotlightPad);
        const grow = (right - left) * spotlightGrow;
        light.classList.toggle("is-wide", left - grow < 0 || right + grow > screen);
        light.style.top = `${scrollY + r.top - spotlightPad}px`;
        light.style.left = `${scrollX + left}px`;
        light.style.width = `${right - left}px`;
        light.style.height = `${r.height + 2 * spotlightPad}px`;
        if (performance.now() < until) requestAnimationFrame(follow);
        else { light.remove(); lit = null; }
    };
    follow();
};
