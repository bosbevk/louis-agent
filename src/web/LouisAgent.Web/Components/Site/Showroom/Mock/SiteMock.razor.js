// A mocked website goes to the place its agent showed (F127.1, a module beside its component, ADR 0093): the mock's
// own page scrolls, never the visitor's, so the place stands a little under the top of what the mock shows; smoothly,
// unless the visitor asked for less motion. A place the page doesn't have is left alone.
const room = 16;

// A new page of the mocked website opens at its top (F127.2), as a page a visitor goes to does; at once, since the
// page itself fades in.
export function toTop(page) {
    page.scrollTo({ top: 0 });
}

// What a live agent on the mocked website reads (F127.2), as the guide's widget reads a real page: the words of the
// page the mock is on, folded and cut at six thousand characters, and every page's places in the order the website
// holds them: each page other than home by its address and heading, marked "(page)", then the places on it, each by
// its address and heading. A hidden page's places are read too, so the agent can show any of them.
export function pageOf(page, url, title) {
    const fold = (s) => (s ?? "").replace(/\s+/g, " ").trim();
    const text = fold(page.innerText).slice(0, 6000);
    const areas = [...page.querySelectorAll(".mock-view[data-page], [data-place]")].flatMap((area) => {
        if (area.dataset.place) {
            const heading = area.querySelector(".mock-section-heading, .mock-card-title");
            return [{ id: area.dataset.place, heading: fold(heading?.textContent) }];
        }
        if (area.dataset.page === "/") return [];
        const heading = fold(area.querySelector(".mock-page-heading")?.textContent);
        return [{ id: area.dataset.page, heading: `${heading} (page)` }];
    });
    return { url, title, text, areas };
}

export function showPlace(page, path) {
    const place = [...page.querySelectorAll("[data-place]")].find((element) => element.dataset.place === path);
    if (!place) return;
    const top = place.getBoundingClientRect().top - page.getBoundingClientRect().top + page.scrollTop - room;
    const still = matchMedia("(prefers-reduced-motion: reduce)").matches;
    page.scrollTo({ top: Math.max(0, top), behavior: still ? "auto" : "smooth" });
}
