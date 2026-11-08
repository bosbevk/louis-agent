// What the visitor is reading (F115, a module beside its component, ADR 0093): the page's address, its title and
// the visible text of its main part, whitespace folded, cut at six thousand characters, so the guide in the corner
// can answer about the page in front of the visitor. Read when a message is sent, so the page is the one rendered.
export function pageText() {
    const main = document.querySelector("main") ?? document.body;
    const text = (main?.innerText ?? "").replace(/\s+/g, " ").trim().slice(0, 6000);
    return { url: location.pathname, title: document.title, text };
}
