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
