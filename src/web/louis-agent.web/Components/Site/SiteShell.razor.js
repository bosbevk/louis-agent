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
    const a = e.target.closest?.(".site a[href^='#']");
    if (!a) return;
    const id = a.getAttribute("href");
    if (id === "#top") { e.preventDefault(); scrollTo({ top: 0, behavior: still() ? "auto" : "smooth" }); return; }
    const target = id.length > 1 ? document.getElementById(id.slice(1)) : null;
    if (!target) return;
    e.preventDefault();
    goTo(target);
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
const slideRoom = () => innerHeight - 160;
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
            if (band.getBoundingClientRect().height > slideRoom() + 2) band.classList.add("is-paged");
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
    const step = stepOf(body), page = Math.round(body.scrollLeft / step) + Number(button.dataset.step);
    body.scrollTo({ left: Math.max(0, page * step), behavior: still() ? "auto" : "smooth" });
    setTimeout(() => count(band), still() ? 0 : 450);
};

// Hangs the listeners once and observes what the latest render added; safe to call after every render. The click is
// heard in the capture phase, before Blazor's own handler: a "#area" link resolves against the page's base, so left
// to Blazor it would open the home page from any other page (F118); handled here first, Blazor leaves it alone.
export function attach() {
    if (listeners.length === 0) {
        on(window, "scroll", scrolled, { passive: true });
        on(window, "resize", scrolled);
        on(window, "resize", remeasure);
        on(document, "click", follow, true);
        on(document, "click", turn);
    }
    observe();
    scrolled();
    paginate();
}

// Takes everything down when the frame goes.
export function detach() {
    listeners.forEach(off => off());
    listeners = [];
    seen?.disconnect();
    seen = null;
}
