// A mocked website goes to the place its agent showed (F127.1, a module beside its component, ADR 0093): the mock's
// own page scrolls, never the visitor's, so the place stands a little under the top of what the mock shows; smoothly,
// unless the visitor asked for less motion. A place the page doesn't have is left alone.
const room = 16;

// A new page of the mocked website opens at its top (F127.2), as a page a visitor goes to does; at once, since the
// page itself fades in.
export function toTop(page) {
    page.scrollTo({ top: 0 });
}

export function showPlace(page, path) {
    const place = [...page.querySelectorAll("[data-place]")].find((element) => element.dataset.place === path);
    if (!place) return;
    const top = place.getBoundingClientRect().top - page.getBoundingClientRect().top + page.scrollTop - room;
    const still = matchMedia("(prefers-reduced-motion: reduce)").matches;
    page.scrollTo({ top: Math.max(0, top), behavior: still ? "auto" : "smooth" });
}
