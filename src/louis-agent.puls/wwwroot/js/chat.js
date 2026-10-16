window.louisChat = {
    // Follows the stream only while the reader is already at the bottom, so scrolling up to read isn't interrupted.
    scrollToBottom(element, force) {
        if (!element) return;
        const nearBottom = element.scrollHeight - element.scrollTop - element.clientHeight < 120;
        if (force || nearBottom) element.scrollTop = element.scrollHeight;
    },

    // Highlights fenced code blocks in a rendered answer. Blazor replaces a block's markup only when its markdown changes,
    // so finished blocks keep their highlighting and a block that is still streaming is redone as it grows.
    highlightBlocks(container) {
        const hljs = window.hljs;
        if (!hljs || !container) return;
        for (const code of container.querySelectorAll("pre code:not([data-highlighted])")) {
            const language = [...code.classList].find(c => c.startsWith("language-"))?.slice("language-".length);
            if (code.textContent.length > 100000 || (language && !hljs.getLanguage(language))) {
                code.dataset.highlighted = "skipped";
            } else if (language) {
                hljs.highlightElement(code);
            } else if (code.textContent.length <= 20000) {
                code.innerHTML = hljs.highlightAuto(code.textContent).value;
                code.dataset.highlighted = "auto";
            }
        }
    },
};

// Enter sends, Shift+Enter adds a new line (IME composition is left alone); the box grows with what's typed.
document.addEventListener("keydown", (e) => {
    if (e.target.id !== "composer-input" || e.key !== "Enter" || e.shiftKey || e.isComposing) return;
    e.preventDefault();
    document.getElementById("send-button")?.click();
});

document.addEventListener("input", (e) => {
    if (e.target.id !== "composer-input") return;
    e.target.style.height = "auto";
    e.target.style.height = Math.min(e.target.scrollHeight, 240) + "px";
});

// The sidebar: drag its edge to resize (220-480px, remembered), double-click the edge to reset, hide and show it.
window.pulsUi = {
    minWidth: 220,
    maxWidth: 480,
    defaultWidth: 280,

    setWidth(width) {
        const clamped = Math.round(Math.min(this.maxWidth, Math.max(this.minWidth, width)));
        document.documentElement.style.setProperty("--sidebar-w", clamped + "px");
        try { localStorage.setItem("puls.sidebarWidth", clamped); } catch { }
    },

    toggleSidebar() {
        const hidden = document.documentElement.classList.toggle("sidebar-hidden");
        try { localStorage.setItem("puls.sidebarHidden", hidden ? "1" : "0"); } catch { }
    },

    // The Werk panel on a wide screen (F20): hidden or shown, remembered.
    toggleWork() {
        const hidden = document.documentElement.classList.toggle("work-hidden");
        try { localStorage.setItem("puls.workHidden", hidden ? "1" : "0"); } catch { }
    },
};

document.addEventListener("pointerdown", (e) => {
    const handle = e.target.closest?.(".resizer");
    if (!handle) return;
    e.preventDefault();
    handle.setPointerCapture(e.pointerId);
    document.documentElement.classList.add("resizing");
    const move = (ev) => pulsUi.setWidth(ev.clientX);
    const up = () => {
        document.documentElement.classList.remove("resizing");
        handle.removeEventListener("pointermove", move);
        handle.removeEventListener("pointerup", up);
    };
    handle.addEventListener("pointermove", move);
    handle.addEventListener("pointerup", up);
});

document.addEventListener("dblclick", (e) => {
    if (e.target.closest?.(".resizer")) pulsUi.setWidth(pulsUi.defaultWidth);
});

// On a phone the sidebar is an offcanvas: close it once a chat is picked or a new one started.
document.addEventListener("click", (e) => {
    if (!e.target.closest?.(".chat-item a, .new-chat")) return;
    const sidebar = document.getElementById("sidebar");
    if (sidebar && window.bootstrap) window.bootstrap.Offcanvas.getInstance(sidebar)?.hide();
});

// The spotlight on cards follows the mouse (app.css reads --mx/--my); a .feature card or a suggestion also tilts a
// little toward it (--rx/--ry/--ty, F24) and settles back when the pointer leaves.
const litCards = ".suggestion, .delegation, .tool, .card, .q, .feature", tilting = ".feature, .suggestion";
document.addEventListener("pointermove", (e) => {
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
}, { passive: true });

document.addEventListener("pointerout", (e) => {
    const card = e.target.closest?.(tilting);
    if (!card || card.contains(e.relatedTarget)) return;
    card.style.setProperty("--rx", "0deg"); card.style.setProperty("--ry", "0deg"); card.style.setProperty("--ty", "0");
}, { passive: true });

// A cost counts up to its value (F24): every [data-count] whose value changed since it was last shown runs from the
// old number to the new one over 900 ms, keeping the text's own prefix, decimals and separator ("R0,47", "$0.01").
// Blazor writes the text and the attribute; it only touches the text node again when the value changes, so the frames
// written here stay. The frames go into Blazor's own text node (its value, not the element's textContent), so the node
// Blazor holds is the one on screen and its next write lands there. Called after each render of the chat page; a
// reduced-motion browser jumps straight to the value.
pulsUi.countCosts = () => {
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
};

// Focus a field once Blazor has drawn it (the chat search, the rename field); select its text when asked.
pulsUi.focus = (selector, select) => {
    const field = document.querySelector(selector);
    if (!field) return;
    field.focus();
    if (select) field.select();
};

// Ctrl K (Cmd K on a Mac) opens the chat search, wherever the focus is.
document.addEventListener("keydown", (e) => {
    if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === "k") {
        e.preventDefault();
        document.getElementById("search-button")?.click();
    }
});

// The profile menu's Instellings: show the sidebar if it's hidden, then open its settings.
pulsUi.openSettings = () => {
    if (document.documentElement.classList.contains("sidebar-hidden")) pulsUi.toggleSidebar();
    const settings = document.querySelector(".sidebar-settings");
    if (!settings) return;
    settings.open = true;
    settings.scrollIntoView({ block: "end", behavior: "smooth" });
};

// Animations although the system asks for less (F24): data-motion="on" on <html> (app.css lifts its reduced-motion
// rules under it; index.html applies it before the first paint), remembered in this browser as puls.motion.
pulsUi.motion = () => document.documentElement.dataset.motion === "on";
pulsUi.reducedMotion = () => matchMedia("(prefers-reduced-motion: reduce)").matches;
pulsUi.setMotion = (on) => {
    if (on) document.documentElement.dataset.motion = "on"; else delete document.documentElement.dataset.motion;
    try { if (on) localStorage.setItem("puls.motion", "on"); else localStorage.removeItem("puls.motion"); } catch { }
};

// Where the user is (F22): kept in this browser only.
pulsUi.loadLocation = () => { try { return localStorage.getItem("puls.location"); } catch { return null; } };
pulsUi.saveLocation = (place) => {
    try { if (place) localStorage.setItem("puls.location", place); else localStorage.removeItem("puls.location"); } catch { }
};
// Puls's language (CL9): the voice a new chat opens with, remembered in this browser as puls.language.
pulsUi.loadLanguage = () => { try { return localStorage.getItem("puls.language"); } catch { return null; } };
pulsUi.saveLanguage = (code) => { try { localStorage.setItem("puls.language", code); } catch { } };
pulsUi.timeZone = () => { try { return Intl.DateTimeFormat().resolvedOptions().timeZone || null; } catch { return null; } };

// "Gebruik my ligging": the browser's position (it asks the user once), named by OpenStreetMap's Nominatim at town level.
// One request per press, as Nominatim's usage policy asks; the browser sends its own Referer and User-Agent. The position
// goes to two decimals (about 1 km), all a town needs.
pulsUi.locate = () => new Promise((resolve) => {
    if (!navigator.geolocation) return resolve({ place: null, error: "Hierdie blaaier kan nie jou ligging gee nie." });
    navigator.geolocation.getCurrentPosition(async (position) => {
        try {
            const { latitude, longitude } = position.coords;
            const url = `https://nominatim.openstreetmap.org/reverse?format=jsonv2&zoom=10&accept-language=af,en`
                + `&lat=${latitude.toFixed(2)}&lon=${longitude.toFixed(2)}`;   // ~1 km: a town, not a street
            const response = await fetch(url, { headers: { "Accept": "application/json" } });
            if (!response.ok) return resolve({ place: null, error: "OpenStreetMap kon nie die plek benoem nie." });
            const address = (await response.json()).address || {};
            const town = address.city || address.town || address.village || address.municipality || address.suburb || address.county;
            const place = [town, address.state || address.province, address.country].filter(Boolean).join(", ");
            resolve(place ? { place, error: null } : { place: null, error: "OpenStreetMap kon nie die plek benoem nie." });
        } catch {
            resolve({ place: null, error: "Kon nie OpenStreetMap bereik nie." });
        }
    }, (error) => resolve({
        place: null,
        error: error.code === 1 ? "Jy het nie toestemming gegee vir jou ligging nie." : "Kon nie jou ligging kry nie.",
    }), { timeout: 10000, maximumAge: 600000 });
});

// A starter in the message box (F20): focus it with the cursor at the end, and grow the box to fit.
pulsUi.focusEnd = (selector) => {
    const box = document.querySelector(selector);
    if (!box) return;
    box.focus();
    box.setSelectionRange(box.value.length, box.value.length);
    box.style.height = "auto";
    box.style.height = Math.min(box.scrollHeight, 240) + "px";
};

// Copy a message (F20): true when the browser allowed it.
pulsUi.copy = async (text) => {
    try { await navigator.clipboard.writeText(text); return true; } catch { return false; }
};

// Show the Werk panel (an answer's sources): unhide it on a wide screen, slide it in on a smaller one.
pulsUi.showWork = () => {
    if (window.matchMedia("(min-width: 1200px)").matches) {
        if (document.documentElement.classList.contains("work-hidden")) pulsUi.toggleWork();
        document.querySelector(".work-sources")?.scrollIntoView({ block: "nearest", behavior: "smooth" });
    } else if (window.bootstrap) {
        window.bootstrap.Offcanvas.getOrCreateInstance(document.getElementById("work")).show();
    }
};

