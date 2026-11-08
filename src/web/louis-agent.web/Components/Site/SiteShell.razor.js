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

// On scroll: the progress line's width, the header compact past the top.
const scrolled = () => {
    const progress = $("#site-progress");
    const max = document.documentElement.scrollHeight - innerHeight;
    if (progress) progress.style.width = (max > 0 ? scrollY / max * 100 : 0) + "%";
    $(".site-header")?.classList.toggle("compact", scrollY > 40);
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

// Hangs the listeners once and observes what the latest render added; safe to call after every render. The click is
// heard in the capture phase, before Blazor's own handler: a "#area" link resolves against the page's base, so left
// to Blazor it would open the home page from any other page (F118); handled here first, Blazor leaves it alone.
export function attach() {
    if (listeners.length === 0) {
        on(window, "scroll", scrolled, { passive: true });
        on(window, "resize", scrolled);
        on(document, "click", follow, true);
    }
    observe();
    scrolled();
}

// Takes everything down when the frame goes.
export function detach() {
    listeners.forEach(off => off());
    listeners = [];
    seen?.disconnect();
    seen = null;
}
