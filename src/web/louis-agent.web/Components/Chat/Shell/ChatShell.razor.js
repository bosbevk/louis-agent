// The chat shell's page-wide behaviour (CL31, beside ChatShell; moved whole from chat.js): the sidebar and the work
// panel, the cards' spotlight and tilt, the costs that count up, focus and copy, and the listeners the page hangs
// on the document while the shell is drawn. attach() when the shell first draws, detach() when it goes; the admin
// pages, which have no shell, get none of it.

const minWidth = 220, maxWidth = 480, defaultWidth = 280;

// The sidebar: drag its edge to resize (220-480px, remembered), double-click the edge to reset, hide and show it.
export function setWidth(width) {
    const clamped = Math.round(Math.min(maxWidth, Math.max(minWidth, width)));
    document.documentElement.style.setProperty("--sidebar-w", clamped + "px");
    try { localStorage.setItem("chat.sidebarWidth", clamped); } catch { }
}

export function toggleSidebar() {
    const hidden = document.documentElement.classList.toggle("sidebar-hidden");
    try { localStorage.setItem("chat.sidebarHidden", hidden ? "1" : "0"); } catch { }
}

// The work panel on a wide screen: hidden or shown, remembered.
export function toggleWork() {
    const hidden = document.documentElement.classList.toggle("work-hidden");
    try { localStorage.setItem("chat.workHidden", hidden ? "1" : "0"); } catch { }
}

// Show the work panel (an answer's sources): unhide it on a wide screen, slide it in on a smaller one.
export function showWork() {
    if (window.matchMedia("(min-width: 1200px)").matches) {
        if (document.documentElement.classList.contains("work-hidden")) toggleWork();
        document.querySelector(".work-sources")?.scrollIntoView({ block: "nearest", behavior: "smooth" });
    } else if (window.bootstrap) {
        window.bootstrap.Offcanvas.getOrCreateInstance(document.getElementById("work")).show();
    }
}

// The profile menu's settings: show the sidebar if it's hidden, then open its settings.
export function openSettings() {
    if (document.documentElement.classList.contains("sidebar-hidden")) toggleSidebar();
    const settings = document.querySelector(".sidebar-settings");
    if (!settings) return;
    settings.open = true;
    settings.scrollIntoView({ block: "end", behavior: "smooth" });
}

// A cost counts up to its value (F24): every [data-count] whose value changed since it was last shown runs from the
// old number to the new one over 900 ms, keeping the text's own prefix, decimals and separator ("R0,47", "$0.01").
// Blazor writes the text and the attribute; it only touches the text node again when the value changes, so the frames
// written here stay. The frames go into Blazor's own text node (its value, not the element's textContent), so the node
// Blazor holds is the one on screen and its next write lands there. Called after each render of the chat page; a
// reduced-motion browser jumps straight to the value.
export function countCosts() {
    const write = (el, text) => { const node = el.firstChild; if (node && node.nodeType === 3 && node === el.lastChild) node.nodeValue = text; else el.textContent = text; };
    const still = matchMedia("(prefers-reduced-motion: reduce)").matches && document.documentElement.dataset.motion !== "on";
    for (const el of document.querySelectorAll("[data-count]")) {
        const target = el.dataset.count;
        if (el.dataset.shown === target) continue;
        const m = /(\d[\d\s]*)([.,](\d+))?/.exec(target);
        const from = parseFloat((el.dataset.shown || "0").replace(/[^\d.,]/g, "").replace(/\s/g, "").replace(",", ".")) || 0;
        el.dataset.shown = target;
        if (!m || still) { write(el, target); continue; }
        const to = parseFloat(m[1].replace(/\s/g, "") + (m[2] ? "." + m[3] : "")), decimals = m[3]?.length ?? 0, sep = m[2]?.[0] ?? ".";
        const prefix = target.slice(0, m.index), suffix = target.slice(m.index + m[0].length), t0 = performance.now();
        const tick = (t) => {
            if (el.dataset.shown !== target) return;      // a newer value took over
            const p = Math.min(1, (t - t0) / 900), e = 1 - Math.pow(1 - p, 3), v = from + (to - from) * e;
            write(el, prefix + v.toFixed(decimals).replace(".", sep) + suffix);
            if (p < 1) requestAnimationFrame(tick);
        };
        requestAnimationFrame(tick);
    }
}

// Text placed in the message box (F20): focus it with the cursor at the end, and grow the box to fit.
export function focusEnd(selector) {
    const box = document.querySelector(selector);
    if (!box) return;
    box.focus();
    box.setSelectionRange(box.value.length, box.value.length);
    box.style.height = "auto";
    box.style.height = Math.min(box.scrollHeight, 240) + "px";
}

// Copy a message (F20): true when the browser allowed it.
export async function copy(text) {
    try { await navigator.clipboard.writeText(text); return true; } catch { return false; }
}

// The browser's time zone, for a new chat's session.
export function timeZone() {
    try { return Intl.DateTimeFormat().resolvedOptions().timeZone || null; } catch { return null; }
}

// The listeners the shell hangs on the document while it is drawn.
let listeners = [];
const on = (type, handler, options) => {
    document.addEventListener(type, handler, options);
    listeners.push(() => document.removeEventListener(type, handler, options));
};

// Enter sends, Shift+Enter adds a new line (IME composition is left alone); the box grows with what's typed.
const enterSends = (e) => {
    if (e.target.id !== "composer-input" || e.key !== "Enter" || e.shiftKey || e.isComposing) return;
    e.preventDefault();
    document.getElementById("send-button")?.click();
};
const grow = (e) => {
    if (e.target.id !== "composer-input") return;
    e.target.style.height = "auto";
    e.target.style.height = Math.min(e.target.scrollHeight, 240) + "px";
};

// A shop's product photo in an answer opens its original in a dialog (F34, Louis): the src Markdown.cs gave it as
// data-original, on the same allowlisted host. Esc, a click on the photo or beside it, closes it.
const openPhoto = (e) => {
    const thumb = e.target.closest?.(".markdown img[data-original]");
    if (!thumb) return;
    const dialog = document.createElement("dialog");
    dialog.className = "photo-dialog";
    const photo = document.createElement("img");
    photo.src = thumb.dataset.original;
    photo.alt = thumb.alt;
    const caption = document.createElement("p");
    caption.textContent = thumb.alt;
    dialog.append(photo, caption);
    dialog.addEventListener("click", () => dialog.close());
    dialog.addEventListener("close", () => dialog.remove());
    document.body.append(dialog);
    dialog.showModal();
};

// The sidebar's edge: a drag resizes it, a double-click resets it.
const startResize = (e) => {
    const handle = e.target.closest?.(".resizer");
    if (!handle) return;
    e.preventDefault();
    handle.setPointerCapture(e.pointerId);
    document.documentElement.classList.add("resizing");
    const move = (ev) => setWidth(ev.clientX);
    const up = () => {
        document.documentElement.classList.remove("resizing");
        handle.removeEventListener("pointermove", move);
        handle.removeEventListener("pointerup", up);
    };
    handle.addEventListener("pointermove", move);
    handle.addEventListener("pointerup", up);
};
const resetWidth = (e) => { if (e.target.closest?.(".resizer")) setWidth(defaultWidth); };

// On a phone the sidebar is an offcanvas: close it once a chat is picked or a new one started.
const closeOffcanvas = (e) => {
    if (!e.target.closest?.(".chat-item a, .new-chat")) return;
    const sidebar = document.getElementById("sidebar");
    if (sidebar && window.bootstrap) window.bootstrap.Offcanvas.getInstance(sidebar)?.hide();
};

// The spotlight on cards follows the mouse (app.css reads --mx/--my); a .feature card or a suggestion also tilts a
// little toward it (--rx/--ry/--ty, F24) and settles back when the pointer leaves.
const litCards = ".suggestion, .delegation, .tool, .card, .q, .feature", tilting = ".feature, .suggestion";
const spotlight = (e) => {
    const card = e.target.closest?.(litCards);
    if (!card) return;
    const box = card.getBoundingClientRect(), x = e.clientX - box.left, y = e.clientY - box.top;
    card.style.setProperty("--mx", x + "px");
    card.style.setProperty("--my", y + "px");
    if (card.matches(tilting)) {
        card.style.setProperty("--ry", ((x / box.width) - .5) * 6 + "deg");
        card.style.setProperty("--rx", (.5 - (y / box.height)) * 6 + "deg");
        card.style.setProperty("--ty", "-4px");
    }
};
const settle = (e) => {
    const card = e.target.closest?.(tilting);
    if (!card || card.contains(e.relatedTarget)) return;
    card.style.setProperty("--rx", "0deg"); card.style.setProperty("--ry", "0deg"); card.style.setProperty("--ty", "0");
};

// Ctrl K (Cmd K on a Mac) opens the chat search, wherever the focus is.
const searchShortcut = (e) => {
    if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === "k") {
        e.preventDefault();
        document.getElementById("search-button")?.click();
    }
};

// Hangs the listeners when the shell first draws; a second attach replaces the first.
export function attach() {
    detach();
    on("keydown", enterSends);
    on("input", grow);
    on("click", openPhoto);
    on("pointerdown", startResize);
    on("dblclick", resetWidth);
    on("click", closeOffcanvas);
    on("pointermove", spotlight, { passive: true });
    on("pointerout", settle, { passive: true });
    on("keydown", searchShortcut);
}

// Takes them down when the shell goes.
export function detach() {
    listeners.forEach(remove => remove());
    listeners = [];
}
