// The section index's own behaviour (F118, a module beside its component, ADR 0093): the entries in the order their
// sections stand on the page, whatever order they registered in, and the one in view marked as the page scrolls.
// The frame's module scrolls a section into view on a click; this one only reads the page.
const $$ = (s, r = document) => [...r.querySelectorAll(s)];
let listeners = [];
let activeClass = "active";
const on = (target, type, fn, opts) => {
    target.addEventListener(type, fn, opts);
    listeners.push(() => target.removeEventListener(type, fn, opts));
};

// The ids in the order their elements stand in the document; one not on the page yet keeps its place, at the end.
export function order(ids) {
    const present = ids.filter(id => document.getElementById(id));
    const follows = (a, b) => document.getElementById(a).compareDocumentPosition(document.getElementById(b)) & Node.DOCUMENT_POSITION_FOLLOWING;
    present.sort((a, b) => follows(a, b) ? -1 : 1);
    return [...present, ...ids.filter(id => !present.includes(id))];
}

// The link whose section's top has passed the line just under the header is the one in view: the same place a jump
// lands a section, so a jump always lights what it went to, and at the top of the page the first section is lit. A
// line halfway down the screen left a short first section never lit on a tall screen (F118, Louis: "that one never
// activates"). At the page's end the last section can't reach the line, so it is the one in view (FX82).
const inViewLine = 140;
const spy = () => {
    const links = $$(".site-index a");
    let active = links.length ? 0 : -1;
    links.forEach((a, k) => {
        const s = document.getElementById(a.getAttribute("href").slice(1));
        if (s && s.getBoundingClientRect().top <= inViewLine) active = k;
    });
    if (links.length && scrollY + innerHeight >= document.documentElement.scrollHeight - 2) active = links.length - 1;
    links.forEach((a, k) => a.classList.toggle(activeClass, k === active));
};

// Hangs the listeners once, under the class the component's styles light, and marks what is in view now; called
// after every draw, since a mark made while the page was still short points at the wrong link once it has grown.
export function attach(cls) {
    activeClass = cls;
    if (listeners.length === 0) {
        on(window, "scroll", spy, { passive: true });
        on(window, "resize", spy);
    }
    spy();
}

// Takes the listeners down when the index goes.
export function detach() {
    listeners.forEach(off => off());
    listeners = [];
}
