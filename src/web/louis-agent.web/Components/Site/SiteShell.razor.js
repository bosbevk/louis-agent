// A site frame's motion (F110, the general half of a demo's landing script, a module beside its component, ADR 0093):
// the progress line, the compact header, in-page links that land their section centred, and the reveals a block
// plays once when it enters; the section index has its own module beside it (F118). A site marks what moves:
// .reveal; the frame attaches after every render, so a page that just drew is observed, and attaching twice is one
// set of listeners. Everything honours the system's "less motion" unless the viewer switched motion on.
import { motion, reducedMotion } from "../../js/preferences.js";

const $ = (s, r = document) => r.querySelector(s);
const $$ = (s, r = document) => [...r.querySelectorAll(s)];
let listeners = [];
let seen = null;
const on = (target, type, fn, opts) => {
    target.addEventListener(type, fn, opts);
    listeners.push(() => target.removeEventListener(type, fn, opts));
};
const still = () => reducedMotion() && !motion();

// An in-page link lands its section centred when it fits, else just under the header; one smooth move.
const headerOffset = 96;
const goTo = (target) => {
    const r = target.getBoundingClientRect(), fits = r.height <= innerHeight - headerOffset;
    const top = scrollY + r.top - (fits ? (innerHeight - r.height) / 2 : headerOffset);
    scrollTo({ top: Math.max(0, top), behavior: still() ? "auto" : "smooth" });
};
const follow = (e) => {
    const a = e.target.closest?.(".site a[href^='#'], .site a[data-section]");
    if (!a) return;
    const href = a.getAttribute("href");
    if (href === "#top") { e.preventDefault(); scrollTo({ top: 0, behavior: still() ? "auto" : "smooth" }); return; }
    const id = a.dataset.section ?? href.slice(1);
    const target = id ? document.getElementById(id) : null;
    if (!target) return;
    // A section's own address (ADR 0107) is a move within this page: no router, the address bar shows where it went.
    e.preventDefault();
    if (a.dataset.section) show(href);
    goTo(target);
};

// Every place on the page has its own address (F118, ADR 0107): the bar shows it as the visitor opens a card or
// turns a page, held for a moment so the index's scroll-following doesn't take it straight back.
let base = null;
const hold = (ms) => { document.documentElement.dataset.addressHeld = String(Date.now() + ms); };
const show = (address) => {
    hold(1200);
    if (location.pathname !== address) history.replaceState(history.state, "", address);
};
const cardOpened = (e) => {
    const card = e.target.closest?.(".site [data-card]");
    if (!card || !base) return;
    setTimeout(() => { if (card.getAttribute("aria-expanded") === "true") show(`${base}/${card.id}`); }, 0);
};
// A turned page's address; the first section, "top", is the page itself, so its pages follow the page's address.
const turned = (band, page) => {
    if (!base || !band.id) return;
    const place = band.id === "top" ? base : `${base}/${band.id}`;
    if (page > 1) show(`${place}/${page}`);
    else show(band.id === "top" && base === "/home" ? "/" : place);
};

// A page opened on a place's address goes to it once it is drawn: a card opens, a paged section turns to its page.
// The address is held while it goes, so it isn't lost while the page loads.
const arriveAt = (place) => {
    const [id, page] = place.split("/");
    const started = performance.now();
    hold(2500);
    const attempt = () => {
        const target = document.getElementById(id);
        if (!target) { if (performance.now() - started < 2000) requestAnimationFrame(attempt); return; }
        if (target.matches("[data-card]") && target.getAttribute("aria-expanded") !== "true") target.click();
        const band = target.closest(".band");
        const body = page && band?.classList.contains("is-paged") ? $(".band-body", band) : null;
        if (body) body.scrollTo({ left: (Number(page) - 1) * stepOf(body), behavior: "auto" });
        goTo(target.matches("[data-card]") ? target.closest(".card-deck") ?? target : target);
    };
    setTimeout(attempt, 60);
};

// On scroll: the progress line's width, the header compact past the top, the way back up shown a screen down (F118).
const scrolled = () => {
    const progress = $("#site-progress");
    const max = document.documentElement.scrollHeight - innerHeight;
    if (progress) progress.style.width = (max > 0 ? scrollY / max * 100 : 0) + "%";
    $(".site-header")?.classList.toggle("compact", scrollY > 40);
    $(".site-up")?.toggleAttribute("data-shown", scrollY > innerHeight * .8);
};

// A block plays its reveal once, when a quarter of it is in view; a page that asks for less motion shows it at rest.
const observe = () => {
    seen ??= new IntersectionObserver(entries => entries.forEach(e => {
        if (!e.isIntersecting) return;
        e.target.classList.add("in");
        seen.unobserve(e.target);
    }), { threshold: .25 });
    $$(".site .reveal:not(.in):not([data-seen])").forEach(el => { el.dataset.seen = "1"; seen.observe(el); });
};

// On a page of slides, a band with more than a screen of content is paged sideways (F118, Louis's option 2): measured
// once as it stands, marked .is-paged so its body flows into screen-high columns, and its pager shown with "1 / n".
// A band already paged only has its count brought up to date, as an opened card can add a page.
// On a phone the corner's bar takes its height off the bottom (FX113), --site-bar on the frame, 0 elsewhere.
const slideRoom = () => innerHeight - 160 - (parseFloat(getComputedStyle($(".site") ?? document.body)
    .getPropertyValue("--site-bar")) || 0);
const stepOf = (body) => body.clientWidth + (parseFloat(getComputedStyle(body).columnGap) || 0);
const count = (band) => {
    const body = $(".band-body", band), pager = $(".band-pager", band);
    if (!body || !pager) return;
    const step = stepOf(body), pages = Math.max(1, Math.round((body.scrollWidth + step - body.clientWidth) / step));
    const page = Math.min(pages, Math.round(body.scrollLeft / step) + 1);
    $(".band-pages", pager).textContent = `${page} / ${pages}`;
    const [back, on] = $$(".band-page", pager);
    back.disabled = page <= 1;
    on.disabled = page >= pages;
    pager.hidden = pages <= 1;
};
const paginate = () => {
    $$(".layout-slides .site-main > section.band").forEach(band => {
        if (!band.dataset.measured) {
            band.dataset.measured = "1";
            // A band that must stay whole (F122, data-whole) is never paged, however tall it is.
            const tall = band.getBoundingClientRect().height > slideRoom() + 2;
            if (tall && !band.hasAttribute("data-whole")) band.classList.add("is-paged");
            // The count follows the body however it is turned: its pager, or the guide naming a page.
            const body = $(".band-body", band);
            if (body && !body.dataset.counted) {
                body.dataset.counted = "1";
                body.addEventListener("scroll", () => count(band), { passive: true });
            }
        }
        if (band.classList.contains("is-paged")) count(band);
    });
};
// A new size measures every band again, as it stands.
const remeasure = () => {
    $$(".site-main > section.band").forEach(band => {
        band.classList.remove("is-paged");
        delete band.dataset.measured;
        const pager = $(".band-pager", band);
        if (pager) pager.hidden = true;
    });
    paginate();
};
// The pager's buttons move the band's body one column back or on, in one smooth move.
const turn = (e) => {
    const button = e.target.closest?.(".band-page");
    if (!button) return;
    const band = button.closest(".band"), body = band && $(".band-body", band);
    if (!body) return;
    const step = stepOf(body), page = Math.max(0, Math.round(body.scrollLeft / step) + Number(button.dataset.step));
    body.scrollTo({ left: page * step, behavior: still() ? "auto" : "smooth" });
    setTimeout(() => count(band), still() ? 0 : 450);
    turned(band, page + 1);
};

// Hangs the listeners once and observes what the latest render added; safe to call after every render. The click is
// heard in the capture phase, before Blazor's own handler: a "#area" link resolves against the page's base, so left
// to Blazor it would open the home page from any other page (F118); handled here first, Blazor leaves it alone. It
// is told what a place's address starts with on this page, and the place the address names when a new page opened
// on one (ADR 0107).
export function attach(pageBase, place) {
    if (listeners.length === 0) {
        on(window, "scroll", scrolled, { passive: true });
        on(window, "resize", scrolled);
        on(window, "resize", remeasure);
        on(document, "click", follow, true);
        on(document, "click", turn);
        on(document, "click", cardOpened);
    }
    observe();
    scrolled();
    paginate();
    if (pageBase !== base) {
        base = pageBase;
        if (place) arriveAt(place);
    }
}

// Takes everything down when the frame goes.
export function detach() {
    listeners.forEach(off => off());
    listeners = [];
    seen?.disconnect();
    seen = null;
}
