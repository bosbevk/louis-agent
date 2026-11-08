// What the visitor is reading (F115, a module beside its component, ADR 0093): the page's address, its title and
// the visible text of its main part, whitespace folded, cut at six thousand characters, so the guide in the corner
// can answer about the page in front of the visitor. Read when a message is sent, so the page is the one rendered.
export function pageText() {
    const main = document.querySelector("main") ?? document.body;
    const text = (main?.innerText ?? "").replace(/\s+/g, " ").trim().slice(0, 6000);
    // The page's named areas (F117): every section with an id, with the heading it carries (its kicker, else its h2).
    const areas = [...document.querySelectorAll("section[id]")].map((section) => ({
        id: section.id,
        heading: (section.querySelector(".kicker, h1, h2, h3")?.textContent ?? "").replace(/\s+/g, " ").trim(),
    }));
    return { url: location.pathname, title: document.title, text, areas };
}

// Takes the visitor to an area the guide named (F117): waits for the page to have it (the router draws the new page
// a moment after the address changes), scrolls it into view and lights it up for four seconds with a class the web's
// styles draw as a glow that fades. Gives up quietly after two seconds if the area never appears.
export function showArea(id) {
    const started = performance.now();
    const attempt = () => {
        const area = document.getElementById(id);
        if (area) {
            area.scrollIntoView({ behavior: "smooth", block: "start" });
            area.classList.add("area-shown");
            setTimeout(() => area.classList.remove("area-shown"), 4000);
            return;
        }
        if (performance.now() - started < 2000) requestAnimationFrame(attempt);
    };
    attempt();
}
